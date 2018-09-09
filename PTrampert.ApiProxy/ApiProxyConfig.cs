using System.Collections.Generic;

namespace PTrampert.ApiProxy
{
    public class ApiProxyConfig : Dictionary<string, ApiConfig>
    {
    }

    public class ApiConfig
    {
        private string baseUrl;

        public string BaseUrl
        {
            get => baseUrl.TrimEnd('/');
            set => baseUrl = value;
        }

        public string AuthType { get; set; }

        public IDictionary<string, string> AuthProps { get; set; } = new Dictionary<string, string>();

        public IEnumerable<string> ResponseHeaders { get; set; } = new List<string>();

        public IEnumerable<string> RequestHeaders { get; set; } = new List<string>();
    }
}
