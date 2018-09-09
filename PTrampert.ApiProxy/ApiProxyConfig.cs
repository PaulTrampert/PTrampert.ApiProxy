using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PTrampert.ApiProxy.Exceptions;

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

        public IDictionary<string, string> AuthProps { get; set; }
    }
}
