using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace PTrampert.ApiProxy
{
    /// <summary>
    /// Interface for providing an authentication header for API calls.
    /// </summary>
    /// <remarks>
    /// Implementations of <see cref="IAuthentication"/> must provide an <see cref="AuthenticationHeaderValue"/> when <see cref="GetAuthenticationHeader(CancellationToken)"/>
    /// is called. A return value of <value>null</value> is valid if the header could not be generated, but the API request should still be attempted. Otherwise,
    /// the call should throw.
    /// </remarks>
    public interface IAuthentication
    {
        /// <summary>
        /// Generate an <see cref="AuthenticationHeaderValue"/> for a proxied API call.
        /// </summary>
        /// <param name="cancellationToken">
        /// Signals that the proxied request was aborted, typically because the client disconnected. The proxy passes
        /// <see cref="Microsoft.AspNetCore.Http.HttpContext.RequestAborted"/>. Implementations that do I/O, such as fetching
        /// or refreshing a token from an identity provider, should pass it on or observe it, and may throw an
        /// <see cref="System.OperationCanceledException"/> once it is cancelled. Implementations that do no I/O may ignore it.
        /// </param>
        /// <returns>The <see cref="AuthenticationHeaderValue"/></returns>
        Task<AuthenticationHeaderValue> GetAuthenticationHeader(CancellationToken cancellationToken);
    }
}
