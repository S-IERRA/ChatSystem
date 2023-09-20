using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ChatSystem.Logic.Models.Websocket;

public record SocketUser(Socket UnderSocket) : IDisposable 
{
    public readonly CancellationTokenSource UserCancellation = new CancellationTokenSource();

    //public IPAddress UserIp { get; set; 
    public bool IsIdentified = false;
    public Guid? SessionId;
        
    private uint _packetId = 1;
        
    public void Dispose()
    {
        UserCancellation.Cancel();
        UnderSocket.Close();
            
        UserCancellation.Dispose();

        GC.SuppressFinalize(this);
    }

    private async Task SendData(WebSocketOpcodes webSocketOpCode, WebSocketEvents? eventType = null, string? dataSerialized = null)
    {
        if (!UnderSocket.Connected)
            Dispose();
            
        var socketMessage = new WebSocketMessage(webSocketOpCode, dataSerialized, eventType, null);

        string messageSerialized = JsonSerializer.Serialize(socketMessage);

        byte[] dataCompressed = Encoding.UTF8.GetBytes(messageSerialized);//GZip.Compress(messageSerialized, _packetId++);

        await UnderSocket.SendAsync(dataCompressed, SocketFlags.None);
    }

    public async Task Send(WebSocketOpcodes webSocketOpCode) 
        => await SendData(webSocketOpCode);
        
    public async Task Send(WebSocketEvents eventType)
        => await SendData(WebSocketOpcodes.Event, eventType);
        
    public async Task Send(WebSocketOpcodes webSocketOpCode, WebSocketEvents eventType) 
        => await SendData(webSocketOpCode, eventType);
        
    public async Task Send(WebSocketOpcodes webSocketOpCode, string message) 
        => await SendData(webSocketOpCode, null, message);

    public async Task Send(WebSocketOpcodes webSocketOpCode, object message)
    {
        string jsonMessage = JsonSerializer.Serialize(message);
            
        await SendData(webSocketOpCode, null, jsonMessage);
    }
        
    public async Task Send(WebSocketEvents eventType, object message)
    {
        if (message is not string)
            message = JsonSerializer.Serialize(message);
            
        await SendData(WebSocketOpcodes.Event, eventType, (string)message);
    }
}