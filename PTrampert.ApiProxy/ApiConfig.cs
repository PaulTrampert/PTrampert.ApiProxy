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
        private string wsBaseUrl;

        /// <summary>
        /// The BaseUrl for a proxied API. This value is required, and should be a fully qualified URL.
        /// </summary>
        public string BaseUrl
        {
            get => baseUrl?.TrimEnd('/');
            set => baseUrl = value;
        }

        /// <summary>
        /// The base url for WebSockets on a proxied API. This value is required to use WebSockets, but can be omitted otherwise.
        /// </summary>
        public string WsBaseUrl
        {
            get => wsBaseUrl?.TrimEnd('/');
            set => wsBaseUrl = value;
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

        /// <summary>
        /// The largest request body, in bytes, the proxy accepts for this api. Use it to let an upstream api
        /// that accepts large uploads receive bodies over the host's limit (Kestrel's default is 30 MB).
        /// <c>null</c>, the default, keeps the host's limit. A request whose body exceeds the limit fails with
        /// <c>413 Payload Too Large</c> and is not forwarded.
        /// </summary>
        /// <remarks>
        /// The limit is applied through <c>IHttpMaxRequestBodySizeFeature</c>, so it has no effect where the host
        /// does not offer that feature or has already started reading the request body.
        /// </remarks>
        public long? MaxRequestBodySize { get; set; }
    }
}