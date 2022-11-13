using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace PTrampert.ApiProxy.Test;

public class WebSocketProxyTest
{
    private HttpContext Context { get; set; }
    
    private ApiConfig Api { get; set; }
    
    private WebSocketProxy Subject { get; set; }

    [SetUp]
    public void SetUp()
    {
        Subject = new WebSocketProxy();
    }
    
    
}