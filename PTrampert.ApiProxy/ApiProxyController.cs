using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace PTrampert.ApiProxy
{
    public class ApiProxyController : Controller
    {
        [Route("{api}/{*path}")]
        public IActionResult Proxy(string api, string path)
        {
            return NoContent();
        }
    }
}
