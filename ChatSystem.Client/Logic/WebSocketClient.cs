using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ChatSystem.Logic.Models.Websocket;

namespace ChatSystem.Client.Logic;

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
            byte[] buffer = ArrayPool.Rent(512);

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
        
        //ToDO: Test Client.DontFragment = true;
        
        //Resolve dns
        //IPAddress addresses = IPAddress.Parse("12")//Dns.GetHostAddresses("0.tcp.eu.ngrok.io")[0];
        var endPoint = new IPEndPoint(IPAddress.Loopback, 8787);

        Client.Connect(endPoint);

        ReceiveMessages();
    }

    private async Task SendData(WebSocketOpcodes webSocketOpCode, RedisEventTypes? eventType = null, string? dataSerialized = default)
    {
        if (!Client.Connected)
            return;

        WebSocketMessage message = new(webSocketOpCode, dataSerialized, eventType, _userSession);

        string messageSerialized = JsonSerializer.Serialize(message);
        byte[] dataCompressed = Compress(messageSerialized);

        await Client.SendAsync(dataCompressed, SocketFlags.None);
    }

    private static byte[] Int2Byte(int number)
    {
        var bytes = new byte[2];
        bytes[0] = (byte)(number & 0xFF);
        bytes[1] = (byte)((number >> 8) & 0xFF);
        return bytes;
    }

    private static byte[] Compress(string data)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] length = Int2Byte(data.Length);

        byte[] prepended = length.Concat(dataBytes).ToArray();

        /*using MemoryStream ms = new();
        using (GZipStream zip = new(ms, CompressionMode.Compress, true))
        {
            zip.Write(prepended, 0, prepended.Length);
        }*/

        return prepended; //ms.ToArray();
    }

    public async Task Send(WebSocketOpcodes webSocketOpCode)
        => await SendData(webSocketOpCode);

    public async Task Send(RedisEventTypes eventType)
        => await SendData(WebSocketOpcodes.Event, eventType);

    public async Task Send(WebSocketOpcodes webSocketOpCode, RedisEventTypes eventType)
        => await SendData(webSocketOpCode, eventType);

    public async Task Send(WebSocketOpcodes webSocketOpCode, string? message)
        => await SendData(webSocketOpCode, null, message);

    public async Task Send(WebSocketOpcodes webSocketOpCode, object message)
    {
        string jsonMessage = JsonSerializer.Serialize(message);
        
        await SendData(webSocketOpCode, null, jsonMessage);
    }

    public async Task Send(RedisEventTypes eventType, object message)
    {
        string jsonMessage = JsonSerializer.Serialize(message);

        await SendData(WebSocketOpcodes.Event, eventType, jsonMessage);
    }
}