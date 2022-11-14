using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using PTrampert.ApiProxy.Exceptions;

namespace PTrampert.ApiProxy
{
    /// <summary>
    /// Controller that proxies api requests to configured API's behind the server.
    /// This class is public so that it can be found by ASP.NET Core, but should not be invoked directly by consuming code.
    /// </summary>
    public sealed class ApiProxyController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly IAuthenticationFactory authFactory;
        private readonly ILogger<ApiProxyController> log;
        private readonly ApiProxyConfig proxyConfig;
        private readonly IWebSocketProxy webSocketProxy;

        /// <summary>
        /// Constructor for <see cref="ApiProxyController"/>
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> used to make requests to downstream API's.</param>
        /// <param name="proxyConfig">The <see cref="ApiProxyConfig"/> containing configured API's.</param>
        /// <param name="authFactory">The <see cref="IAuthenticationFactory"/>.</param>
        /// <param name="log">The <see cref="ILogger{ApiProxyController}"/></param>
        public ApiProxyController(HttpClient httpClient, IOptions<ApiProxyConfig> proxyConfig, IAuthenticationFactory authFactory, IWebSocketProxy webSocketProxy, ILogger<ApiProxyController> log = null)
        {
            this.httpClient = httpClient;
            this.authFactory = authFactory;
            this.webSocketProxy = webSocketProxy;
            this.log = log;
            this.proxyConfig = proxyConfig.Value;
        }

        /// <summary>
        /// Proxy a request to the downstream API.
        /// </summary>
        /// <param name="api">The API to proxy to.</param>
        /// <param name="path">The path of the request.</param>
        /// <returns>The response.</returns>
        public async Task<IActionResult> Proxy(string api, string path)
        {
            if (!proxyConfig.ContainsKey(api))
            {
                throw new ProxyException($"No API named '{api}' configured.", (int)HttpStatusCode.NotFound);
            }

            var apiConfig = proxyConfig[api];

            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                if (apiConfig.WsBaseUrl == null)
                {
                    throw new ProxyException($"API {api} not configured to use WebSockets.",
                        (int) HttpStatusCode.BadRequest);
                }
                await webSocketProxy.Proxy(HttpContext, apiConfig, path);
                return new EmptyResult();
            }
            
            var response = await MakeRequest(apiConfig, path);
            
            Response.StatusCode = (int) response.StatusCode;
            foreach (var responseHeaderKey in apiConfig.ResponseHeaders)
            {
                if (response.Headers.Contains(responseHeaderKey))
                {
                    Response.Headers.Add(responseHeaderKey, new StringValues(response.Headers.GetValues(responseHeaderKey).ToArray()));
                }
            }

            if (response.Content?.Headers.ContentLength != null && response.Content.Headers.ContentLength.Value > 0)
            {
                var contentType = response.Content.Headers.ContentType;
                var stream = await response.Content.ReadAsStreamAsync();
                return File(stream, contentType.ToString());
            }
            return new EmptyResult();
        }

        private async Task<HttpResponseMessage> MakeRequest(ApiConfig apiConfig, string path)
        {
            using var request = new HttpRequestMessage(new HttpMethod(Request.Method), new Uri($"{apiConfig.BaseUrl}/{path}{Request.QueryString.Value}"));
            using var content = new StreamContent(Request.Body ?? Stream.Null);
            foreach (var requestHeaderKey in apiConfig.RequestHeaders)
            {
                if (Request.Headers.ContainsKey(requestHeaderKey))
                    request.Headers.Add(requestHeaderKey, request.Headers.GetValues(requestHeaderKey));
            }

            if ((Request.ContentLength ?? 0) > 0)
            {
                request.Content = content;
                request.Content.Headers.ContentType = string.IsNullOrWhiteSpace(Request.ContentType) ?
                    request.Content.Headers.ContentType
                    : new MediaTypeHeaderValue(Request.ContentType);
            }

            var auth = authFactory.BuildAuthentication(apiConfig);
            if (auth != null)
            {
                request.Headers.Authorization = await auth.GetAuthenticationHeader();
            }

            return await httpClient.SendAsync(request);
        }
    }
}
