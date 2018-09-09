using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PTrampert.ApiProxy
{
    /// <summary>
    /// Interface for providing an authentication header for API calls.
    /// </summary>
    /// <remarks>
    /// Implementations of <see cref="IAuthentication"/> must provide an <see cref="AuthenticationHeaderValue"/> when <see cref="GetAuthenticationHeader"/>
    /// is called. A return value of <value>null</value> is valid if the header could not be generated, but the API request should still be attempted. Otherwise,
    /// the call should throw.
    /// </remarks>
    public interface IAuthentication
    {
        /// <summary>
        /// Generate an <see cref="AuthenticationHeaderValue"/> for a proxied API call.
        /// </summary>
        /// <returns>The <see cref="AuthenticationHeaderValue"/></returns>
        Task<AuthenticationHeaderValue> GetAuthenticationHeader();
    }
}
