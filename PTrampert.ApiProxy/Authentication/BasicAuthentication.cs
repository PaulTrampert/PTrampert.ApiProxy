using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PTrampert.ApiProxy.Authentication
{
    /// <summary>
    /// Provides HTTP Basic authentication to the downstream API, using statically configured credentials.
    /// </summary>
    public class BasicAuthentication : IAuthentication
    {
        private AuthenticationHeaderValue header;
        private string id;
        private string secret;

        /// <summary>
        /// The Id or Username.
        /// </summary>
        public string Id
        {
            get => id;
            set
            {
                id = value;
                BuildHeader();
            }
        }

        /// <summary>
        /// The Secret or Password.
        /// </summary>
        public string Secret
        {
            get => secret;
            set
            {
                secret = value;
                BuildHeader();
            }
        }

        /// <summary>
        /// Gets the 
        /// </summary>
        /// <returns></returns>
        public Task<AuthenticationHeaderValue> GetAuthenticationHeader()
        {
            return Task.FromResult(header);
        }

        private void BuildHeader()
        {
            var param = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Id}:{Secret}"));
            header = new AuthenticationHeaderValue("Basic", param);
        }
    }
}
