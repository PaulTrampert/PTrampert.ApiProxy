using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ApiProxyConfig proxyConfig;
        private readonly IWebSocketProxy webSocketProxy;

        /// <summary>
        /// Content headers the proxy sets itself, from the body it forwards, and so never copies from the other side.
        /// </summary>
        internal static readonly IReadOnlySet<string> ProxyOwnedContentHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Content-Length",
            "Content-Type"
        };

        /// <summary>
        /// Constructor for <see cref="ApiProxyController"/>
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> used to make requests to downstream API's.</param>
        /// <param name="proxyConfig">The <see cref="ApiProxyConfig"/> containing configured API's.</param>
        /// <param name="authFactory">The <see cref="IAuthenticationFactory"/>.</param>
        /// <param name="webSocketProxy">The <see cref="IWebSocketProxy"/> to use for proxying web socket requests.</param>
        public ApiProxyController(HttpClient httpClient, IOptions<ApiProxyConfig> proxyConfig, IAuthenticationFactory authFactory, IWebSocketProxy webSocketProxy)
        {
            this.httpClient = httpClient;
            this.authFactory = authFactory;
            this.webSocketProxy = webSocketProxy;
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
            if (!proxyConfig.TryGetValue(api, out var apiConfig))
            {
                throw new ProxyException($"No API named '{api}' configured.", (int)HttpStatusCode.NotFound);
            }

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
            // As with request headers, names are matched case insensitively but passed back exactly as
            // the upstream API spelled them.
            var configuredResponseHeaders = new HashSet<string>(apiConfig.ResponseHeaders, StringComparer.OrdinalIgnoreCase);
            foreach (var (upstreamHeaderKey, upstreamHeaderValues) in response.Headers)
            {
                if (configuredResponseHeaders.Contains(upstreamHeaderKey))
                {
                    Response.Headers.Append(upstreamHeaderKey, new StringValues(upstreamHeaderValues.ToArray()));
                }
            }
            // Content headers (Expires, Last-Modified, Content-Disposition, ...) arrive on the content rather than
            // on the response. Content-Length and Content-Type are left out: ASP.NET Core and the returned
            // FileResult set those for the response the proxy writes, and ApiProxyConfigValidator reserves them.
            foreach (var (upstreamHeaderKey, upstreamHeaderValues) in response.Content.Headers)
            {
                if (configuredResponseHeaders.Contains(upstreamHeaderKey) && !ProxyOwnedContentHeaders.Contains(upstreamHeaderKey))
                {
                    Response.Headers.Append(upstreamHeaderKey, new StringValues(upstreamHeaderValues.ToArray()));
                }
            }

            if (response.Content.Headers.ContentLength is not > 0) return new EmptyResult();
            
            var contentType = response.Content.Headers.ContentType;
            var stream = await response.Content.ReadAsStreamAsync();
            return File(stream, contentType?.ToString() ?? "application/octet-stream");
        }

        private async Task<HttpResponseMessage> MakeRequest(ApiConfig apiConfig, string path)
        {
            using var upstreamRequest = new HttpRequestMessage(new HttpMethod(Request.Method), new Uri($"{apiConfig.BaseUrl}/{path}{Request.QueryString.Value}"));
            
            // Request.Body *can* be null (e.g. GET requests), so we need to use Stream.Null in that case.
            // ReSharper disable once ConstantNullCoalescingCondition
            using var content = new StreamContent(Request.Body ?? Stream.Null);
            // Header names are matched case insensitively, but forwarded exactly as the client spelled them:
            // the upstream API may not treat them case insensitively, whatever the spec says.
            var configuredRequestHeaders = new HashSet<string>(apiConfig.RequestHeaders, StringComparer.OrdinalIgnoreCase);
            foreach (var (incomingHeaderKey, incomingHeaderValues) in Request.Headers)
            {
                if (!configuredRequestHeaders.Contains(incomingHeaderKey)) continue;
                // The proxy sets these itself from the request body; ApiProxyConfigValidator reserves them.
                if (ProxyOwnedContentHeaders.Contains(incomingHeaderKey)) continue;
                // Values are added without validation so that the upstream API receives the bytes the client
                // sent. Headers.Add() would parse strongly typed headers and throw a FormatException on a value
                // it cannot parse (an unbalanced parenthesis in a User-Agent, say), failing the whole request
                // over a header a proxy has no business reinterpreting.
                if (upstreamRequest.Headers.TryAddWithoutValidation(incomingHeaderKey, (IEnumerable<string>)[.. incomingHeaderValues]))
                {
                    continue;
                }

                // Request headers refuse content headers (Content-Disposition, Expires, ...), which belong on the
                // request's content instead. The content is only attached when there is a body, so on a bodyless
                // request a content header has nowhere to go and is quietly not forwarded.
                if (content.Headers.TryAddWithoutValidation(incomingHeaderKey, (IEnumerable<string>)[.. incomingHeaderValues]))
                {
                    continue;
                }

                // Neither the request nor its content accepts the name, so it is not a valid header name at all.
                throw new ProxyException(
                    $"Header '{incomingHeaderKey}' cannot be forwarded as a request header. Remove it from the api's RequestHeaders.",
                    (int)HttpStatusCode.InternalServerError);
            }

            if ((Request.ContentLength ?? 0) > 0)
            {
                upstreamRequest.Content = content;
                if (!string.IsNullOrWhiteSpace(Request.ContentType))
                {
                    // Forwarded verbatim, for the same reason as the headers above: parsing it into a
                    // MediaTypeHeaderValue would throw a FormatException on a value the client sent malformed,
                    // and would reject a valid one that carries parameters, such as a charset.
                    upstreamRequest.Content.Headers.TryAddWithoutValidation("Content-Type", Request.ContentType);
                }
            }

            var auth = authFactory.BuildAuthentication(apiConfig);
            if (auth != null)
            {
                upstreamRequest.Headers.Authorization = await auth.GetAuthenticationHeader();
            }

            return await httpClient.SendAsync(upstreamRequest);
        }
    }
}
