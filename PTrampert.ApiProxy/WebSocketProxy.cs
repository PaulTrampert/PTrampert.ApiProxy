using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PTrampert.ApiProxy;

internal class WebSocketProxy : IWebSocketProxy
{
    private const int BufferSize = 4096;
    
    public async Task Proxy(HttpContext context, ApiConfig api, string path)
    {
        Debug.Assert(api.WsBaseUrl != null);
        using var backEndSocket = await GetClientSocket(new Uri($"{api.WsBaseUrl}/{path}{context.Request.QueryString.Value}"), context.RequestAborted);
        using var frontEndSocket = await context.WebSockets.AcceptWebSocketAsync();
        
        var frontToBack = Proxy(frontEndSocket, backEndSocket, context.RequestAborted);
        var backToFront = Proxy(backEndSocket, frontEndSocket, context.RequestAborted);

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

    public virtual async Task<WebSocket> GetClientSocket(Uri uri, CancellationToken c)
    {
        var backEndSocket = new ClientWebSocket();
        await backEndSocket.ConnectAsync(uri, c);
        return backEndSocket;
    }

    private async Task Proxy(WebSocket from, WebSocket to, CancellationToken c)
    {
        var buffer = new Memory<byte>(new byte[BufferSize]);
        while (!c.IsCancellationRequested && !to.CloseStatus.HasValue && !from.CloseStatus.HasValue)
        {
            var receiveResult = await from.ReceiveAsync(buffer, c);
            if (receiveResult.Count > 0)
                await to.SendAsync(buffer[..receiveResult.Count], receiveResult.MessageType, receiveResult.EndOfMessage, c);
        }
    }
}