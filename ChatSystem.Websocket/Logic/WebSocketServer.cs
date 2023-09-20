using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ChatSystem.Logic.Models.Websocket;
using Org.BouncyCastle.Bcpg;
using Serilog;

namespace ChatSystem.Websocket.Logic;

public class WebSocketServer : IDisposable
{
    private static readonly ArrayPool<byte> ArrayPool = ArrayPool<byte>.Create();

    private static readonly RedisCommunicator RedisCommunicator = new RedisCommunicator();

    private static readonly Socket Listener = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private static readonly IPEndPoint EndPoint = new(IPAddress.Loopback, 8787);

    private static readonly CancellationTokenSource Cts = new CancellationTokenSource();
    
    private static DateTime GetCurrentTime => DateTime.Now;

    public WebSocketServer()
    {
        Listener.Bind(EndPoint);
        Listener.Listen(32);
        
        while (!Cts.Token.IsCancellationRequested)
        {
            Socket socket = Listener.Accept();
            IPEndPoint ip = (IPEndPoint)socket.RemoteEndPoint!; 

            SocketUser socketUser = new(socket);
            
            Log.Information($"New connection from {ip}");
            
            _ = Task.Run(() => VirtualUserHandler(socketUser), socketUser.UserCancellation.Token);
        }
    }

    private async Task VirtualUserHandler(SocketUser socketUser)
    {
        bool receivedAck = false;
        
        async Task HeartBeat()
        {
            while (!socketUser.UserCancellation.IsCancellationRequested)
            {
                await socketUser.Send(WebSocketOpcodes.Heartbeat);

                DateTimeOffset nextAck = DateTimeOffset.Now + TimeSpan.FromSeconds(10);

                while (DateTimeOffset.Now < nextAck && !receivedAck)
                    await Task.Delay(2000);

                if (!receivedAck)
                {
                    if(socketUser.IsIdentified)
                        await RedisCommunicator.SendAsync(RedisOpcodes.Logout, socketUser.SessionId!.Value);
                    
                    await socketUser.Send(WebSocketOpcodes.Logout);
                    socketUser.Dispose();
                    
                    return;
                }

                receivedAck = false;
                await Task.Delay(5000);
            }
        }
        
        //_ = Task.Run(HeartBeat, Cts.Token);

        while (socketUser.UserCancellation.IsCancellationRequested)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(512);

            do
            {
                int received = await socketUser.UnderSocket.ReceiveAsync(new Memory<byte>(buffer), SocketFlags.None);
                if (received < 512 )
                    break;
                
                Array.Resize(ref buffer, buffer.Length + 512);
            } 
            while (socketUser.UnderSocket.Available > 0);

            Console.WriteLine(Encoding.UTF8.GetString(buffer));
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        Listener.Dispose();
        Cts.Dispose();

        Log.Information("Server stopped");
        Log.CloseAndFlush();
    }

    ~WebSocketServer()
    {
        Dispose(false);
    }
}