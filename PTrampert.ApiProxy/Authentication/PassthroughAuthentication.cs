using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PTrampert.ApiProxy.Exceptions;

namespace PTrampert.ApiProxy.Authentication;

/// <summary>
/// Passes the client's own credentials through to the downstream API by forwarding the incoming request's
/// <c>Authorization</c> header as the upstream request's <c>Authorization</c> header.
/// </summary>
/// <remarks>
/// This is the explicit way to say "forward the caller's credentials". Listing <c>Authorization</c> in an
/// api's <see cref="ApiConfig.RequestHeaders"/> without an <see cref="ApiConfig.AuthType"/> has the same effect,
/// but only because nothing else sets the header; that implicit form is deprecated in favour of this type.
/// When the incoming request has no <c>Authorization</c> header, no header is sent upstream. The value is
/// forwarded as the client sent it: everything before the first space is the scheme and everything after it
/// the parameter, which is not reinterpreted.
/// </remarks>
public class PassthroughAuthentication : IAuthentication
{
    private readonly IHttpContextAccessor httpContext;

    /// <summary>
    /// Constructor for <see cref="PassthroughAuthentication"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="IHttpContextAccessor"/> used to read the incoming request.</param>
    public PassthroughAuthentication([NotNull] IHttpContextAccessor httpContext)
    {
        this.httpContext = httpContext;
    }

    /// <summary>
    /// Builds the upstream authentication header from the incoming request's <c>Authorization</c> header.
    /// </summary>
    /// <param name="cancellationToken">
    /// Not observed: the header is copied from the incoming request, so there is no work to cancel.
    /// </param>
    /// <returns>
    /// The incoming <c>Authorization</c> header as an <see cref="AuthenticationHeaderValue"/>, or <value>null</value>
    /// when the incoming request has none.
    /// </returns>
    /// <exception cref="ProxyException">
    /// Thrown with status 400 when the incoming header's scheme is not a valid HTTP token, so it cannot be sent upstream.
    /// </exception>
    public Task<AuthenticationHeaderValue> GetAuthenticationHeader(CancellationToken cancellationToken)
    {
        var incoming = httpContext.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(incoming))
        {
            return Task.FromResult<AuthenticationHeaderValue>(null);
        }

        var separator = incoming.IndexOf(' ');
        var scheme = separator < 0 ? incoming : incoming[..separator];
        var parameter = separator < 0 ? null : incoming[(separator + 1)..];
        try
        {
            return Task.FromResult(new AuthenticationHeaderValue(scheme, parameter));
        }
        catch (FormatException)
        {
            throw new ProxyException(
                "The request's Authorization header has an invalid scheme and cannot be forwarded.",
                (int)HttpStatusCode.BadRequest);
        }
    }
}
