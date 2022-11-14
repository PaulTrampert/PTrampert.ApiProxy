using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PTrampert.ApiProxy;

public interface IWebSocketProxy
{
    Task Proxy(HttpContext context, ApiConfig api, string path);
}