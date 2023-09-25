using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ChatSystem.Logic.Helpers;

namespace ChatSystem.Logic.Models.Websocket;

public record SocketUser(Socket UnderSocket) : IDisposable 
{
    public readonly CancellationTokenSource UserCancellation = new CancellationTokenSource();

    public IPEndPoint? EndPoint = UnderSocket.RemoteEndPoint as IPEndPoint;

    //public IPAddress UserIp { get; set; 
    public bool IsIdentified = false;
    public Guid? SessionId;
    
    public static implicit operator SocketUser(Socket socket) => new SocketUser(socket);
    
    public void Dispose()
    {
        UnderSocket.Close();
        UnderSocket.Dispose();

        GC.SuppressFinalize(this);
    }

    private async Task SendAsync(WebSocketOpcodes webSocketOpCode, RedisEventTypes? eventType = null, string? dataSerialized = null)
    {
        if (!UnderSocket.Connected)
            Dispose();
            
        var socketMessage = new WebSocketMessage(webSocketOpCode, dataSerialized, eventType, null);

        string messageSerialized = JsonSerializer.Serialize(socketMessage);

        byte[] dataCompressed = ByteUtils.Compress(messageSerialized);

        await UnderSocket.SendAsync(dataCompressed, SocketFlags.None);
    }

    public async Task Send(WebSocketOpcodes webSocketOpCode) 
        => await SendAsync(webSocketOpCode);
        
    public async Task Send(RedisEventTypes eventType)
        => await SendAsync(WebSocketOpcodes.Event, eventType);
        
    public async Task Send(WebSocketOpcodes webSocketOpCode, RedisEventTypes eventType) 
        => await SendAsync(webSocketOpCode, eventType);
        
    public async Task Send(WebSocketOpcodes webSocketOpCode, string message) 
        => await SendAsync(webSocketOpCode, null, message);

    public async Task Send(WebSocketOpcodes webSocketOpCode, object message)
    {
        string jsonMessage = JsonSerializer.Serialize(message);
            
        await SendAsync(webSocketOpCode, null, jsonMessage);
    }
        
    public async Task Send(RedisEventTypes eventType, object message)
    {
        if (message is not string)
            message = JsonSerializer.Serialize(message);
            
        await SendAsync(WebSocketOpcodes.Event, eventType, (string)message);
    }
}