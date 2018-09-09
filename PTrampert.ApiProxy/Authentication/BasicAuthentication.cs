using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PTrampert.ApiProxy.Authentication
{
    public class BasicAuthentication : IAuthentication
    {
        public string Id { get; set; }
        public string Secret { get; set; }

        public Task<AuthenticationHeaderValue> GetAuthenticationHeader()
        {
            var param = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Id}:{Secret}"));
            return Task.FromResult(new AuthenticationHeaderValue("Basic", param));
        }
    }
}
