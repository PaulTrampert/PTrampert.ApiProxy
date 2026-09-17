using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;

namespace PTrampert.ApiProxy;

/// <summary>
/// Validates a bound <see cref="ApiProxyConfig"/>, rejecting request and response headers that the proxy
/// handles itself and therefore cannot forward.
/// </summary>
internal class ApiProxyConfigValidator : IValidateOptions<ApiProxyConfig>
{
    /// <summary>
    /// Headers that live on <c>HttpContent.Headers</c> rather than on the message itself, as
    /// <c>System.Net.Http</c> divides them up. Adding one of these to <c>HttpRequestMessage.Headers</c> is a
    /// "misused header name", and none of them ever appear in <c>HttpResponseMessage.Headers</c> either, so
    /// configuring one is never what the caller meant.
    /// </summary>
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
                    failures.Add($"Api '{apiName}' configures reserved request header '{header}'. Content headers describe the request body, which the proxy forwards from the incoming request, so the header cannot be proxied individually. Remove it from RequestHeaders.");
                }
                else if (string.Equals(header, "Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add($"Api '{apiName}' configures reserved request header '{header}'. The Authorization header is set from the configured AuthType. Remove it from RequestHeaders.");
                }
            }

            foreach (var header in (apiConfig.ResponseHeaders ?? Enumerable.Empty<string>()).Where(ContentHeaders.Contains))
            {
                failures.Add($"Api '{apiName}' configures reserved response header '{header}'. Content headers describe the response body, which the proxy forwards from the upstream response, so the header cannot be proxied individually. Remove it from ResponseHeaders.");
            }
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
