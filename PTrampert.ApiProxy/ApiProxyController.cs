using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
            
            ApplyMaxRequestBodySize(apiConfig);

            var response = await MakeRequest(apiConfig, path);
            // The upstream body is streamed rather than buffered, so the response message has to outlive this
            // method: it is disposed once ASP.NET Core has finished writing the response to the client.
            Response.RegisterForDispose(response);

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

            if (HasNoBody(response)) return new EmptyResult();


            var contentType = response.Content.Headers.ContentType;
            var stream = await response.Content.ReadAsStreamAsync();
            return File(stream, contentType?.ToString() ?? "application/octet-stream");
        }

        private void ApplyMaxRequestBodySize(ApiConfig apiConfig)
        {
            var bodySizeFeature = HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (bodySizeFeature == null) return;

            // Raise (or lower) the host's limit the same way an action would, before anything reads the body.
            if (apiConfig.MaxRequestBodySize.HasValue && !bodySizeFeature.IsReadOnly)
            {
                bodySizeFeature.MaxRequestBodySize = apiConfig.MaxRequestBodySize;
            }

            // The server only enforces the limit as the body is read, and the body is read while it is being sent
            // upstream. Reject a declared length over the limit here instead, so the upstream api is never contacted.
            if (Request.ContentLength > bodySizeFeature.MaxRequestBodySize)
            {
                throw new ProxyException(
                    $"Request body of {Request.ContentLength} bytes exceeds the limit of {bodySizeFeature.MaxRequestBodySize} bytes.",
                    StatusCodes.Status413PayloadTooLarge);
            }
        }

        /// <summary>
        /// Decides whether the upstream response carries no body. The upstream Content-Length cannot be relied on,
        /// because a streamed, chunked response has none, so emptiness comes from the request method and status code.
        /// An explicit Content-Length of zero is also treated as empty.
        /// </summary>
        private bool HasNoBody(HttpResponseMessage response)
        {
            return HttpMethods.IsHead(Request.Method)
                || response.StatusCode == HttpStatusCode.NoContent
                || response.StatusCode == HttpStatusCode.NotModified
                || response.Content.Headers.ContentLength == 0;
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
                // Values are added without validation so that the upstream API receives the bytes the client
                // sent. Headers.Add() would parse strongly typed headers and throw a FormatException on a value
                // it cannot parse (an unbalanced parenthesis in a User-Agent, say), failing the whole request
                // over a header a proxy has no business reinterpreting.
                if (!upstreamRequest.Headers.TryAddWithoutValidation(incomingHeaderKey, (IEnumerable<string>)[.. incomingHeaderValues]))
                {
                    // The only remaining reason to be rejected is a misused header name: a content header
                    // configured in RequestHeaders, which belongs to Content.Headers and can never be forwarded
                    // here. That is a configuration error, so say which header caused it.
                    throw new ProxyException(
                        $"Header '{incomingHeaderKey}' cannot be forwarded as a request header. Remove it from the api's RequestHeaders.",
                        (int)HttpStatusCode.InternalServerError);
                }
            }

            if (RequestHasBody())
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

            // ResponseHeadersRead returns as soon as the upstream headers arrive, so the body is streamed to the
            // client as it is received instead of being buffered in memory first.
            return await httpClient.SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead);
        }

        // A chunked request has a body but no Content-Length, so Content-Length alone cannot decide this.
        // The server knows whether the request can carry a body (Kestrel reports it for both framing styles,
        // and for HTTP/2 and HTTP/3); fall back to inspecting the framing headers when it does not say.
        private bool RequestHasBody()
        {
            var bodyDetection = HttpContext.Features?.Get<IHttpRequestBodyDetectionFeature>();
            if (bodyDetection != null) return bodyDetection.CanHaveBody;
            return Request.ContentLength > 0 || Request.Headers.ContainsKey(Microsoft.Net.Http.Headers.HeaderNames.TransferEncoding);
        }
    }
}
