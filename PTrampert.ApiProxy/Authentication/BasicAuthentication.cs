using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PTrampert.ApiProxy.Authentication
{
    public class BasicAuthentication : IAuthentication
    {
        private readonly AuthenticationHeaderValue authHeader;

        public BasicAuthentication(string id, string secret)
        {
            authHeader = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{id}:{secret}")));
        }

        public Task<AuthenticationHeaderValue> GetAuthenticationHeader()
        {
            return Task.FromResult(authHeader);
        }
    }
}
