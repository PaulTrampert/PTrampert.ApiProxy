using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PTrampert.ApiProxy;

public class WebSocketProxy : IWebSocketProxy
{
    private const int BufferSize = 4096;
    
    public async Task Proxy(HttpContext context, ApiConfig api, string path)
    {
        using var backEndSocket = new ClientWebSocket();
        await backEndSocket.ConnectAsync(new Uri($"{api.BaseUrl}/{path}{context.Request.QueryString.Value}"),
            context.RequestAborted);
        using var frontEndSocket = await context.WebSockets.AcceptWebSocketAsync();
        
        var frontToBack = ProxyFrontToBack(frontEndSocket, backEndSocket, context.RequestAborted);
        var backToFront = ProxyBackToFront(frontEndSocket, backEndSocket, context.RequestAborted);

        await Task.WhenAny(frontToBack, backToFront);

        if (frontEndSocket.CloseStatus.HasValue && !backEndSocket.CloseStatus.HasValue)
        {
            await backEndSocket.CloseAsync(frontEndSocket.CloseStatus.Value, frontEndSocket.CloseStatusDescription, context.RequestAborted);
        }
        else if (backEndSocket.CloseStatus.HasValue && !frontEndSocket.CloseStatus.HasValue)
        {
            await frontEndSocket.CloseAsync(backEndSocket.CloseStatus.Value, backEndSocket.CloseStatusDescription, context.RequestAborted);
        }
    }

    private async Task ProxyBackToFront(WebSocket frontEnd, ClientWebSocket backEnd, CancellationToken c)
    {
        var buffer = new Memory<byte>(new byte[BufferSize]);
        while (!c.IsCancellationRequested && !frontEnd.CloseStatus.HasValue && !backEnd.CloseStatus.HasValue)
        {
            var receiveResult = await backEnd.ReceiveAsync(buffer, c);
            await frontEnd.SendAsync(buffer[..receiveResult.Count], receiveResult.MessageType, receiveResult.EndOfMessage, c);
        }
    }

    private async Task ProxyFrontToBack(WebSocket frontEnd, WebSocket backEnd, CancellationToken c)
    {
        var buffer = new Memory<byte>(new byte[BufferSize]);
        while (!c.IsCancellationRequested && !frontEnd.CloseStatus.HasValue && !backEnd.CloseStatus.HasValue)
        {
            var receiveResult = await frontEnd.ReceiveAsync(buffer, c);
            await backEnd.SendAsync(buffer[..receiveResult.Count], receiveResult.MessageType, receiveResult.EndOfMessage, c);
        }
    }
}