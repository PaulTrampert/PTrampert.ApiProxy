using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
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
        /// Gets the Basic authentication header built from <see cref="Id"/> and <see cref="Secret"/>.
        /// </summary>
        /// <param name="cancellationToken">
        /// Not observed: the header is built when the credentials are set, so there is no work to cancel.
        /// </param>
        /// <returns>A Basic <see cref="AuthenticationHeaderValue"/></returns>
        public Task<AuthenticationHeaderValue> GetAuthenticationHeader(CancellationToken cancellationToken)
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
