using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ChatSystem.Logic.Models.Websocket;

namespace ChatSystem.Client.Logic;

//ToDo: Convert the client and the server to aa shared file to inherit from
//ToDo: Convert reply tasks to a cached task, kafka seems good in this situation
public class WebSocketClient
{
    private readonly Socket Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    
    private static readonly ArrayPool<byte> ArrayPool = ArrayPool<byte>.Create();

    //Move this to a user config file
    private static Guid _userSession { get; set; }
    private static uint PacketIndex = 1;

    private async void ReceiveMessages()
    {
        byte[] localBuffer = new byte[512];

        for (;;)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(512);

            do
            {
                int received = await Client.ReceiveAsync(new Memory<byte>(buffer), SocketFlags.None);
                if (received < 512 )
                    break;
                
                Array.Resize(ref buffer, buffer.Length + 512);
            } 
            while (Client.Available > 0);

            Console.WriteLine(Encoding.UTF8.GetString(buffer));
        }
    }

    public WebSocketClient()
    {
        Client.DontFragment = true;
        
        //Resolve dns
        IPAddress addresses = Dns.GetHostAddresses("0.tcp.eu.ngrok.io")[0];
        var endPoint = new IPEndPoint(addresses, 8787);

        Client.Connect(endPoint);

        ReceiveMessages();
    }

    private async Task SendData(WebSocketOpcodes webSocketOpCode, WebSocketEvents? eventType = null, string? dataSerialized = default)
    {
        if (!Client.Connected)
            return;

        WebSocketMessage message = new(webSocketOpCode, dataSerialized, eventType, _userSession);

        string messageSerialized = JsonSerializer.Serialize(message);
        byte[] dataCompressed = Encoding.UTF8.GetBytes(messageSerialized);//GZip.Compress(messageSerialized, PacketIndex);

        await Client.SendAsync(dataCompressed, SocketFlags.None);
    }

    public async Task Send(WebSocketOpcodes webSocketOpCode)
        => await SendData(webSocketOpCode);

    public async Task Send(WebSocketEvents eventType)
        => await SendData(WebSocketOpcodes.Event, eventType);

    public async Task Send(WebSocketOpcodes webSocketOpCode, WebSocketEvents eventType)
        => await SendData(webSocketOpCode, eventType);

    public async Task Send(WebSocketOpcodes webSocketOpCode, string? message)
        => await SendData(webSocketOpCode, null, message);

    public async Task Send(WebSocketOpcodes webSocketOpCode, object message)
    {
        string jsonMessage = JsonSerializer.Serialize(message);

        await SendData(webSocketOpCode, null, jsonMessage);
    }

    public async Task Send(WebSocketEvents eventType, object message)
    {
        string jsonMessage = JsonSerializer.Serialize(message);

        await SendData(WebSocketOpcodes.Event, eventType, jsonMessage);
    }
}