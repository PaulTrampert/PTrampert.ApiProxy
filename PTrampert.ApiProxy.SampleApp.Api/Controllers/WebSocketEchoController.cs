using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace PTrampert.ApiProxy.SampleApp.Api.Controllers;

[Route("ws-echo")]
[ApiController]
public class WebSocketEchoController : ControllerBase
{
    public async Task Echo(WebSocket socket)
    {
        var buffer = new Memory<byte>(new byte[4096]);
        while (!HttpContext.RequestAborted.IsCancellationRequested && !socket.CloseStatus.HasValue)
        {
            var receiveResult = await socket.ReceiveAsync(buffer, HttpContext.RequestAborted);
            await socket.SendAsync(buffer[..receiveResult.Count], receiveResult.MessageType,
                receiveResult.EndOfMessage, HttpContext.RequestAborted);
        }
    }
    
    [HttpGet("")]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            await Echo(await HttpContext.WebSockets.AcceptWebSocketAsync());
        }
        else
        {
            Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
    }
}