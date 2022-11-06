using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace PTrampert.ApiProxy.Authentication
{
    /// <summary>
    /// Provides Bearer token authentication to the downstream API using a token stored either
    /// as a user claim, or as an authentication cookie.
    /// </summary>
    /// <remarks>
    /// When using <see cref="UserBearerAuthentication"/> in <see cref="TokenMode.Claims"/> mode, the value of the first <see cref="Claim"/>
    /// found where <see cref="Claim.Type"/> matches <see cref="TokenKey"/> will be used as the Bearer token. When
    /// using <see cref="TokenMode.AuthProps"/> mode, the token will be retrieved using
    /// <see cref="AuthenticationHttpContextExtensions.GetTokenAsync(HttpContext,string)"/>.
    /// </remarks>
    public class UserBearerAuthentication : IAuthentication
    {
        /// <summary>
        /// The token storage mode. Defaults to <see cref="TokenMode.AuthProps"/>.
        /// </summary>
        public string Mode { get; set; } = TokenMode.AuthProps.ToString();

        /// <summary>
        /// The TokenKey. Defaults to <value>"access_token"</value>.
        /// </summary>
        public string TokenKey { get; set; } = "access_token";

        /// <summary>
        /// The auth scheme for the token storage. Only used with <see cref="TokenMode.AuthProps"/>.
        /// </summary>
        public string AuthScheme { get; set; } = null;

        private readonly IHttpContextAccessor httpContext;

        /// <summary>
        /// Constructor for <see cref="UserBearerAuthentication"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="IHttpContextAccessor"/></param>
        public UserBearerAuthentication(IHttpContextAccessor httpContext)
        {
            this.httpContext = httpContext;
        }

        /// <summary>
        /// Generates the bearer token authentication header.
        /// </summary>
        /// <returns>A bearer token <see cref="AuthenticationHeaderValue"/></returns>
        public async Task<AuthenticationHeaderValue> GetAuthenticationHeader()
        {
            string token = null;
            switch ((TokenMode)Enum.Parse(typeof(TokenMode), Mode))
            {
                case TokenMode.Claims:
                    token = httpContext.HttpContext.User.FindFirst(TokenKey).Value;
                    break;
                case TokenMode.AuthProps:
                    token = await httpContext.HttpContext.GetTokenAsync(AuthScheme, TokenKey);
                    break;
            }
            return new AuthenticationHeaderValue("Bearer", token);
        }
    }

    /// <summary>
    /// Enum of valid TokenModes.
    /// </summary>
    public enum TokenMode
    {
        /// <summary>
        /// Token should be found in the authentication properties.
        /// </summary>
        AuthProps,

        /// <summary>
        /// Token should be found in the user claims.
        /// </summary>
        Claims
    }
}
