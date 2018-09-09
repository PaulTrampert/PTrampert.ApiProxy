using System;
using System.Collections.Generic;

namespace PTrampert.ApiProxy
{
    /// <summary>
    /// Dictionary of configured API's that the proxy can handle. Keys are compared with <see cref="StringComparer.OrdinalIgnoreCase"/>.
    /// </summary>
    public class ApiProxyConfig : Dictionary<string, ApiConfig>
    {
        public ApiProxyConfig() : base(StringComparer.OrdinalIgnoreCase)
        {
        }
    }
}
