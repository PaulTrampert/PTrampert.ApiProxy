using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;

namespace PTrampert.ApiProxy;

/// <summary>
/// Decides whether the messages the proxy forwards carry a body. Content-Length alone cannot decide either
/// direction, because a chunked HTTP/1.1 message and an HTTP/2 or HTTP/3 message can carry a body without one.
/// </summary>
internal static class MessageBodyExtensions
{
    /// <summary>
    /// Decides whether the incoming request carries a body. The server knows the request's framing (Kestrel reports
    /// it for chunked and Content-Length bodies, and for HTTP/2 and HTTP/3), so its answer is used when it gives one;
    /// otherwise the framing headers decide.
    /// </summary>
    public static bool HasBody(this HttpRequest request)
    {
        var bodyDetection = request.HttpContext?.Features.Get<IHttpRequestBodyDetectionFeature>();
        if (bodyDetection != null) return bodyDetection.CanHaveBody;
        return request.ContentLength > 0 || request.Headers.ContainsKey(HeaderNames.TransferEncoding);
    }

    /// <summary>
    /// Decides whether an upstream response carries a body. Nothing reports the framing of an
    /// <see cref="HttpResponseMessage"/>, and a streamed response often has no Content-Length, so a body is assumed
    /// unless HTTP semantics rule one out: a response to <c>HEAD</c>, a <c>204</c> or <c>304</c>, or an explicit
    /// Content-Length of zero.
    /// </summary>
    /// <param name="response">The upstream response.</param>
    /// <param name="requestMethod">The method of the request the response answers.</param>
    public static bool HasBody(this HttpResponseMessage response, string requestMethod)
    {
        return !HttpMethods.IsHead(requestMethod)
            && response.StatusCode != HttpStatusCode.NoContent
            && response.StatusCode != HttpStatusCode.NotModified
            && response.Content.Headers.ContentLength != 0;
    }
}
