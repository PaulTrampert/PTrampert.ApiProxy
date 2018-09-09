using System.Collections.Generic;
using PTrampert.ApiProxy.Authentication;

namespace PTrampert.ApiProxy
{
    /// <summary>
    /// The configuration for a proxied API.
    /// </summary>
    public class ApiConfig
    {
        private string baseUrl;

        /// <summary>
        /// The BaseUrl for a proxied API. This value is required, and should be a fully qualified URL.
        /// </summary>
        public string BaseUrl
        {
            get => baseUrl.TrimEnd('/');
            set => baseUrl = value;
        }


        /// <summary>
        /// The type name of the <see cref="IAuthentication"/> type to use for this api.
        /// </summary>
        /// <seealso cref="BasicAuthentication"/>
        /// <seealso cref="UserBearerAuthentication"/>
        public string AuthType { get; set; }

        /// <summary>
        /// Dictionary of props specific to the specified <see cref="AuthType"/>.
        /// </summary>
        public IDictionary<string, string> AuthProps { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Collection of header keys to proxy down from the response.
        /// </summary>
        public IEnumerable<string> ResponseHeaders { get; set; } = new List<string>();

        /// <summary>
        /// Collection of header keys to proxy up into the request.
        /// </summary>
        public IEnumerable<string> RequestHeaders { get; set; } = new List<string>();
    }
}