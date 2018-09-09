using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using PTrampert.ApiProxy.Exceptions;

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
            string token;
            switch ((TokenMode)Enum.Parse(typeof(TokenMode), Mode))
            {
                case TokenMode.Claims:
                    token = httpContext.HttpContext.User.FindFirst(TokenKey).Value;
                    break;
                case TokenMode.AuthProps:
                    token = await httpContext.HttpContext.GetTokenAsync(TokenKey);
                    break;
                default:
                    throw new InvalidTokenModeException(Mode);
            }
            return new AuthenticationHeaderValue("Bearer", token);
        }

        public virtual Task<string> GetTokenFromAuthProps(string tokenKey)
        {
            return httpContext.HttpContext.GetTokenAsync(tokenKey);
        }
    }

    public enum TokenMode
    {
        AuthProps,
        Claims
    }
}
