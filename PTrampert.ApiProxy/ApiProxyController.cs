using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace PTrampert.ApiProxy
{
    public class ApiProxyController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly ApiProxyConfig proxyConfig;

        public ApiProxyController(HttpClient httpClient, IOptions<ApiProxyConfig> proxyConfig)
        {
            this.httpClient = httpClient;
            this.proxyConfig = proxyConfig.Value;
        }

        [Route("{api}/{*path}")]
        public async Task<IActionResult> Proxy(string api, string path)
        {
            if (!proxyConfig.ContainsKey(api))
            {
                return NotFound(new ApiError($"Unknown api: {api}"));
            }
            return NoContent();
        }
    }
}
