using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PTrampert.ApiProxy;

/// <summary>
/// Interface for proxying WebSockets. Only public to allow usage in ApiProxyController. Not intended for
/// outside consumption.
/// </summary>
public interface IWebSocketProxy
{
    /// <summary>
    /// Proxy a web socket connection on the provided api and path.
    /// </summary>
    /// <param name="context">The HttpContext for the request. This request *must* be a WebSockets request.</param>
    /// <param name="api">The api to proxy to.</param>
    /// <param name="path">The path on the api to proxy to.</param>
    /// <returns>A Task that will complete when the connection is closed.</returns>
    Task Proxy(HttpContext context, ApiConfig api, string path);
}