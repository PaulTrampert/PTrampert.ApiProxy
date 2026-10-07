using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PTrampert.ApiProxy.Authentication;

namespace PTrampert.ApiProxy;

/// <summary>
/// Validates a bound <see cref="ApiProxyConfig"/>, rejecting request and response headers the proxy cannot
/// forward: <c>Content-Length</c> and <c>Content-Type</c>, which the proxy sets itself from the body it forwards,
/// and <c>Authorization</c> for an api whose configured authentication sets it.
/// </summary>
internal class ApiProxyConfigValidator : IValidateOptions<ApiProxyConfig>
{
    /// <summary>
    /// The content headers the proxy owns, in both directions. Every other content header (<c>Expires</c>,
    /// <c>Last-Modified</c>, <c>Content-Disposition</c>, ...) is forwarded on the message's content, but these
    /// two describe the body the proxy itself writes: the upstream request's <c>Content-Type</c> comes from the
    /// incoming request and its length from the body, and on the way back ASP.NET Core sets
    /// <c>Content-Length</c> and the returned <c>FileResult</c> sets <c>Content-Type</c>. Configuring one would
    /// promise a copy the proxy never makes.
    /// </summary>
    private static readonly IReadOnlySet<string> ReservedContentHeaders = ApiProxyController.ProxyOwnedContentHeaders;

    /// <summary>
    /// The <see cref="ApiConfig.AuthType"/> value that selects <see cref="PassthroughAuthentication"/>.
    /// </summary>
    private static readonly string PassthroughAuthType = $"{typeof(PassthroughAuthentication).FullName}, {typeof(PassthroughAuthentication).Assembly.GetName().Name}";

    private readonly ILogger<ApiProxyConfigValidator> logger;

    /// <summary>
    /// Constructor for <see cref="ApiProxyConfigValidator"/>.
    /// </summary>
    /// <param name="logger">The logger that receives deprecation warnings for configurations that still work.</param>
    public ApiProxyConfigValidator(ILogger<ApiProxyConfigValidator> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Validate a bound <see cref="ApiProxyConfig"/>.
    /// </summary>
    /// <remarks>
    /// <c>Authorization</c> is only reserved for an api that configures an <c>AuthType</c>, matching
    /// <see cref="DefaultAuthenticationFactory.BuildAuthentication"/>, which builds no authentication when
    /// <see cref="ApiConfig.AuthType"/> is null. An api without one may forward the client's Authorization
    /// header, so rejecting that configuration would break working setups. That implicit passthrough is
    /// deprecated in favour of <see cref="PassthroughAuthentication"/>, so it is logged as a warning instead.
    /// <see cref="IValidateOptions{TOptions}"/> has no channel for warnings, which is why it is logged rather
    /// than returned.
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
                if (ReservedContentHeaders.Contains(header))
                {
                    failures.Add($"Api '{apiName}' configures reserved request header '{header}'. The proxy sets it on the upstream request from the body it forwards, so a configured value would never be copied. Remove it from RequestHeaders.");
                }
                else if (string.Equals(header, "Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    if (apiConfig.AuthType != null)
                    {
                        failures.Add($"Api '{apiName}' configures reserved request header '{header}', but also configures AuthType '{apiConfig.AuthType}', which sets that header. The configured authentication wins, so the forwarded header would be discarded. Remove it from RequestHeaders.");
                    }
                    else
                    {
                        logger.LogWarning(
                            "Api '{ApiName}' forwards the client's Authorization header by listing '{Header}' in RequestHeaders. This implicit passthrough is deprecated and will be rejected in a future major version. Remove it from RequestHeaders and set AuthType to '{PassthroughAuthType}' instead.",
                            apiName, header, PassthroughAuthType);
                    }
                }
            }

            foreach (var header in (apiConfig.ResponseHeaders ?? Enumerable.Empty<string>()).Where(ReservedContentHeaders.Contains))
            {
                failures.Add($"Api '{apiName}' configures reserved response header '{header}'. The proxy sets it on the response from the body it returns, so the upstream value is never copied. Remove it from ResponseHeaders.");
            }
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
