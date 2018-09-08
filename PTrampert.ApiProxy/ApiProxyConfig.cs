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
    }
}
