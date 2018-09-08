using System;
using System.Collections.Generic;
using System.Text;

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
