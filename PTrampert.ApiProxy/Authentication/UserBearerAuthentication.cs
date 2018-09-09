using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace PTrampert.ApiProxy.Authentication
{
    public class UserBearerAuthentication : IAuthentication
    {
        public string Mode { get; set; } = TokenMode.AuthProps.ToString();

        public string TokenKey { get; set; } = "access_token";

        private readonly IHttpContextAccessor httpContext;

        public UserBearerAuthentication(IHttpContextAccessor httpContext)
        {
            this.httpContext = httpContext;
        }

        public async Task<AuthenticationHeaderValue> GetAuthenticationHeader()
        {
            string token = null;
            switch ((TokenMode)Enum.Parse(typeof(TokenMode), Mode))
            {
                case TokenMode.Claims:
                    token = httpContext.HttpContext.User.FindFirst(TokenKey).Value;
                    break;
                case TokenMode.AuthProps:
                    token = await httpContext.HttpContext.GetTokenAsync(TokenKey);
                    break;
            }
            return new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public enum TokenMode
    {
        AuthProps,
        Claims
    }
}
