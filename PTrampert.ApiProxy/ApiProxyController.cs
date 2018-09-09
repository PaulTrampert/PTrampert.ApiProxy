using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using PTrampert.ApiProxy.Exceptions;

namespace PTrampert.ApiProxy
{
    public class ApiProxyController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly IAuthenticationBuilder authBuilder;
        private readonly ApiProxyConfig proxyConfig;

        public ApiProxyController(HttpClient httpClient, IOptions<ApiProxyConfig> proxyConfig, IAuthenticationBuilder authBuilder)
        {
            this.httpClient = httpClient;
            this.authBuilder = authBuilder;
            this.proxyConfig = proxyConfig.Value;
        }

        [Route("{api}/{*path}")]
        public async Task Proxy(string api, string path)
        {
            if (!proxyConfig.ContainsKey(api))
            {
                throw new ProxyException($"No API named '{api}' configured.", (int)HttpStatusCode.NotFound);
            }

            var apiConfig = proxyConfig[api];
            
            var request = new HttpRequestMessage(new HttpMethod(Request.Method), new Uri($"{apiConfig.BaseUrl}/{path}{Request.QueryString.Value}"));
            if ((Request.ContentLength ?? 0) > 0)
            {
                request.Content = new StreamContent(Request.Body);
                request.Content.Headers.ContentType = string.IsNullOrWhiteSpace(Request.ContentType) ? 
                    request.Content.Headers.ContentType 
                    : new MediaTypeHeaderValue(Request.ContentType);
            }

            var auth = authBuilder.BuildAuthentication(apiConfig);
            if (auth != null)
            {
                request.Headers.Authorization = await auth.GetAuthenticationHeader();
            }

            var response = await httpClient.SendAsync(request);

            Response.StatusCode = (int) response.StatusCode;
            foreach (var header in response.Headers)
            {
                Response.Headers[header.Key] = new StringValues(header.Value.ToArray());
            }

            if (response.Content != null)
            {
                Response.ContentType = response.Content.Headers.ContentType.ToString();
                Response.Body = await response.Content.ReadAsStreamAsync();
            }
        }
    }
}
