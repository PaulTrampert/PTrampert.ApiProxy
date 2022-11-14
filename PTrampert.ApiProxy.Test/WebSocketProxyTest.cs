using System;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;

namespace PTrampert.ApiProxy.Test;

public class WebSocketProxyTest
{
    private Mock<HttpContext> Context { get; set; }
    
    private Mock<HttpRequest> Request { get; set; }
    
    private ApiConfig Api { get; set; }
    
    private CancellationToken CancellationToken { get; set; }
    
    private Mock<WebSocket> FrontEndSocket { get; set; }
    
    private Mock<WebSocket> BackEndSocket { get; set; }
    
    private Mock<WebSocketManager> WebSocketManager { get; set; }
    
    private Mock<WebSocketProxy> Subject { get; set; }

    [SetUp]
    public void SetUp()
    {
        Api = new ApiConfig
        {
            BaseUrl = "http://example.com"
        };
        CancellationToken = CancellationToken.None;

        Request = new Mock<HttpRequest>();
        Request.SetupAllProperties();
        
        Context = new Mock<HttpContext>();
        Context.SetupAllProperties();
        Context.SetupGet(c => c.RequestAborted)
            .Returns(CancellationToken);
        Context.SetupGet(c => c.Request)
            .Returns(Request.Object);

        FrontEndSocket = new Mock<WebSocket>();
        FrontEndSocket.SetupGet(s => s.CloseStatus)
            .Returns(WebSocketCloseStatus.NormalClosure);
        BackEndSocket = new Mock<WebSocket>();
        BackEndSocket.SetupGet(s => s.CloseStatus)
            .Returns(WebSocketCloseStatus.NormalClosure);

        WebSocketManager = new Mock<WebSocketManager>();
        WebSocketManager.Setup(m => m.AcceptWebSocketAsync())
            .ReturnsAsync(FrontEndSocket.Object);

        Context.SetupGet(c => c.WebSockets)
            .Returns(WebSocketManager.Object);
        
        // Using a mock here so we can mock out the "GetClientSocket" method.
        Subject = new Mock<WebSocketProxy>();
        Subject.Setup(p => p.GetClientSocket(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BackEndSocket.Object);
    }

    [TestCase("http://example.com", "some/path", "?some=param", "http://example.com/some/path?some=param")]
    [TestCase("http://www.example.com", "some/path", "?other=param", "http://www.example.com/some/path?other=param")]
    [TestCase("http://example.com", "", null, "http://example.com")]
    [TestCase("http://example.com", null, null, "http://example.com")]
    public async Task ItGetsTheBackEndSocketUsingTheProvidedPathAndBaseUrl(string baseUrl, string path, string query, string expectedUrl)
    {
        Api.BaseUrl = baseUrl;
        Request.SetupGet(r => r.QueryString)
            .Returns(new QueryString(query));
        await Subject.Object.Proxy(Context.Object, Api, path);
        
        Subject.Verify(s => s.GetClientSocket(new Uri(expectedUrl), CancellationToken));
    }

    [Test]
    public async Task ItAcceptsTheFrontEndWebSocketRequest()
    {
        await Subject.Object.Proxy(Context.Object, Api, "some/path");
        
        WebSocketManager.Verify(m => m.AcceptWebSocketAsync());
    }

    [Test]
    public async Task ItProxiesDataFromFrontEndToBackEnd()
    {
        FrontEndSocket.SetupGet(s => s.CloseStatus)
            .Returns((WebSocketCloseStatus?) null);
        BackEndSocket.SetupGet(s => s.CloseStatus)
            .Returns((WebSocketCloseStatus?) null);

        var data = new byte[] {1, 2, 3, 4};
        
        FrontEndSocket.Setup(s => s.ReceiveAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
            .Callback((Memory<byte> buffer, CancellationToken _) =>
            {
                MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment);
                for(var i = 0; i < data.Length; i++)
                {
                    segment[i] = data[i];
                }

                FrontEndSocket.SetupGet(s => s.CloseStatus)
                    .Returns(WebSocketCloseStatus.NormalClosure);
            }).ReturnsAsync(new ValueWebSocketReceiveResult(4, WebSocketMessageType.Binary, true));

        await Subject.Object.Proxy(Context.Object, Api, "some/path");
        
        BackEndSocket.Verify(s => s.SendAsync(It.Is<ReadOnlyMemory<byte>>(m => data.SequenceEqual(m.ToArray())), WebSocketMessageType.Binary, true, CancellationToken));
    }
    
    [Test]
    public async Task ItProxiesDataFromBackEndToFrontEnd()
    {
        BackEndSocket.SetupGet(s => s.CloseStatus)
            .Returns((WebSocketCloseStatus?) null);
        FrontEndSocket.SetupGet(s => s.CloseStatus)
            .Returns((WebSocketCloseStatus?) null);

        var data = new byte[] {1, 2, 3, 4};
        
        FrontEndSocket.Setup(s => s.ReceiveAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
            .Callback((Memory<byte> buffer, CancellationToken _) =>
            {
                MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment);
                for(var i = 0; i < data.Length; i++)
                {
                    segment[i] = data[i];
                }
            }).ReturnsAsync(new ValueWebSocketReceiveResult(4, WebSocketMessageType.Binary, true));

        var frontEndClosed = false;

        FrontEndSocket.SetupGet(s => s.CloseStatus)
            .Callback(() => frontEndClosed = !frontEndClosed)
            .Returns(() => frontEndClosed ? WebSocketCloseStatus.NormalClosure : null);
        
        BackEndSocket.Setup(s => s.ReceiveAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
            .Callback((Memory<byte> buffer, CancellationToken _) =>
            {
                MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment);
                for(var i = 0; i < data.Length; i++)
                {
                    segment[i] = data[i];
                }
            }).ReturnsAsync(new ValueWebSocketReceiveResult(4, WebSocketMessageType.Binary, true));
        
        await Subject.Object.Proxy(Context.Object, Api, "some/path");
        
        FrontEndSocket.Verify(s => s.SendAsync(It.Is<ReadOnlyMemory<byte>>(m => data.SequenceEqual(m.ToArray())), WebSocketMessageType.Binary, true, CancellationToken));
    }
}