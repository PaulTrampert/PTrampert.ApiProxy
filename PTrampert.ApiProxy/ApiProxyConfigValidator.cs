using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;

namespace PTrampert.ApiProxy;

/// <summary>
/// Validates a bound <see cref="ApiProxyConfig"/>, rejecting request and response headers the proxy cannot
/// forward: content headers, which <c>System.Net.Http</c> keeps on the message's content rather than on the
/// message, and <c>Authorization</c> for an api whose configured authentication sets it.
/// </summary>
internal class ApiProxyConfigValidator : IValidateOptions<ApiProxyConfig>
{
    /// <summary>
    /// The headers <c>System.Net.Http</c> keeps on <c>HttpContent.Headers</c> rather than on the message
    /// itself; this is exactly the set <c>HttpContentHeaders</c> exposes. Neither direction of the proxy can
    /// carry one: <c>HttpRequestMessage.Headers.TryAddWithoutValidation</c> refuses them all as a "misused
    /// header name", so a configured request header fails every request to the api, and an upstream response
    /// puts them on <c>response.Content.Headers</c>, which the proxy never enumerates, so a configured
    /// response header silently forwards nothing.
    /// </summary>
    /// <remarks>
    /// Being reserved does not mean the proxy preserves them some other way. Apart from <c>Content-Type</c>,
    /// which is carried by the returned <c>FileResult</c>, these headers are dropped rather than forwarded.
    /// Teaching the proxy to forward them from <c>Content.Headers</c> is tracked in issue #240; until then,
    /// configuring one promises something the proxy does not do.
    /// </remarks>
    private static readonly HashSet<string> ContentHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Allow",
        "Content-Disposition",
        "Content-Encoding",
        "Content-Language",
        "Content-Length",
        "Content-Location",
        "Content-MD5",
        "Content-Range",
        "Content-Type",
        "Expires",
        "Last-Modified"
    };

    /// <summary>
    /// Validate a bound <see cref="ApiProxyConfig"/>.
    /// </summary>
    /// <remarks>
    /// <c>Authorization</c> is only reserved for an api that configures an <c>AuthType</c>, matching
    /// <see cref="DefaultAuthenticationFactory.BuildAuthentication"/>, which builds no authentication when
    /// <see cref="ApiConfig.AuthType"/> is null. An api without one may forward the client's Authorization
    /// header, so rejecting that configuration would break working setups.
    /// </remarks>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The <see cref="ApiProxyConfig"/> to validate.</param>
    /// <returns>
    /// <see cref="ValidateOptionsResult.Success"/>, or a failure listing every reserved header that was configured.
    /// </returns>
    public ValidateOptionsResult Validate(string name, ApiProxyConfig options)
    {
        var failures = new List<string>();

        foreach (var (apiName, apiConfig) in options)
        {
            foreach (var header in apiConfig.RequestHeaders ?? Enumerable.Empty<string>())
            {
                if (ContentHeaders.Contains(header))
                {
                    failures.Add($"Api '{apiName}' configures reserved request header '{header}'. It is a content header, which belongs to the request's content rather than the request itself, so it cannot be added to the upstream request and would fail every request to this api. Remove it from RequestHeaders.");
                }
                else if (string.Equals(header, "Authorization", StringComparison.OrdinalIgnoreCase) && apiConfig.AuthType != null)
                {
                    failures.Add($"Api '{apiName}' configures reserved request header '{header}', but also configures AuthType '{apiConfig.AuthType}', which sets that header. The configured authentication wins, so the forwarded header would be discarded. Remove it from RequestHeaders.");
                }
            }

            foreach (var header in (apiConfig.ResponseHeaders ?? Enumerable.Empty<string>()).Where(ContentHeaders.Contains))
            {
                failures.Add($"Api '{apiName}' configures reserved response header '{header}'. It is a content header, which arrives on the upstream response's content rather than on the response itself, where the proxy does not look, so configuring it forwards nothing. Remove it from ResponseHeaders.");
            }
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
