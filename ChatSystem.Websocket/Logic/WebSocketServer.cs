using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ChatSystem.Data;
using ChatSystem.Data.Caching;
using ChatSystem.Data.Dtos;
using ChatSystem.Logic.ChatSystem_Logic;
using ChatSystem.Logic.Constants;
using ChatSystem.Logic.Helpers;
using ChatSystem.Logic.Models.Rest;
using ChatSystem.Logic.Models.Websocket;
using ChatSystem.Logic.Websocket;
using Microsoft.EntityFrameworkCore;using Org.BouncyCastle.Bcpg;
using Serilog;
using StackExchange.Redis;
using JsonHelper = ChatSystem.ApiWrapper.Helpers.JsonHelper;

namespace ChatSystem.Websocket.Logic;

public class WebSocketServer : IDisposable
{
    private static readonly ArrayPool<byte> ArrayPool = ArrayPool<byte>.Create();

    private static readonly ConnectionMultiplexer CacheClient = RedisConnectionManager.Connection;
    private static readonly ISubscriber RedisSubscriber = CacheClient.GetSubscriber();

    private static readonly Socket Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private static readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Loopback, 8787);

    private static readonly CancellationTokenSource Cts = new CancellationTokenSource();
    
    private readonly IDbContextFactory<EntityFrameworkContext> _dbContext;

    private static DateTime GetCurrentTime => DateTime.Now;

    public WebSocketServer(IDbContextFactory<EntityFrameworkContext> dbContext)
    {
        _dbContext = dbContext;
        
        Console.WriteLine($"Began server");

        Listener.Bind(EndPoint);
        Listener.Listen(32);
        
        while (!Cts.Token.IsCancellationRequested)
        {
            SocketUser socketUser = Listener.Accept();
            Console.WriteLine($"New connection from {socketUser.EndPoint}");
            
            _ = Task.Run(() => VirtualUserHandler(socketUser), socketUser.UserCancellation.Token);
        }
    }

    private async Task VirtualUserHandler(SocketUser socketUser)
    {
        bool receivedAck = false;

        Guid sessionId = Guid.NewGuid();
        Console.WriteLine(sessionId);
        
        ChannelMessageQueue messageQueue = await RedisSubscriber.SubscribeAsync(sessionId.ToString());
        messageQueue.OnMessage(async message =>
        {
            if (!JsonHelper.TryDeserialize<RedisTransferMessage>(message.Message, out var transferMessage))
                return;

            if (transferMessage.OpCodes != RedisOpcodes.Event)
                return;

            switch (transferMessage.EventType)
            {
                case RedisEventTypes.None:
                    break;

                case RedisEventTypes.DeleteMessage:
                case RedisEventTypes.EditMessage:
                case RedisEventTypes.NewMessage:
                {
                    if (!JsonHelper.TryDeserialize<BasicMessage>(transferMessage.Data, out var basicMessage))
                        return;

                    List<string> channelUsers = await RedisUserCache.GetChannelUsers(basicMessage.Channel.Id);
                    foreach (var _ in channelUsers)
                        await socketUser.Send(transferMessage.EventType, basicMessage);

                    break;
                }

                case RedisEventTypes.Unfriended:
                case RedisEventTypes.Friended:

                case RedisEventTypes.Unblocked:
                case RedisEventTypes.Blocked:

                case RedisEventTypes.CancelFriendRequest:
                case RedisEventTypes.IncomingFriendRequest:
                case RedisEventTypes.OutgoingFriendRequest:
                case RedisEventTypes.DeclinedFriendRequest:
                {
                    if (!JsonHelper.TryDeserialize<BasicChatRelationship>(transferMessage.Data, out var basicRelationship))
                        return;

                    await socketUser.Send(transferMessage.EventType, basicRelationship);

                    break;
                }
            }
        });
        
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
                    if (socketUser.IsIdentified)
                    {
                        await using var context = await _dbContext.CreateDbContextAsync();
                        await context.LogoutUser(sessionId);
                    }
                    
                    await socketUser.Send(WebSocketOpcodes.Logout);
                    socketUser.UserCancellation.Cancel();
                    
                    return;
                }

                receivedAck = false;
                await Task.Delay(5000);
            }
        }
        
        //_ = Task.Run(HeartBeat, Cts.Token);

        while (!socketUser.UserCancellation.IsCancellationRequested)
        {
            byte[] buffer = ArrayPool.Rent(512);
            int totalReceived = 0; // To keep track of the total bytes received.

            do
            {
                int received = await socketUser.UnderSocket.ReceiveAsync(new Memory<byte>(buffer), SocketFlags.None);
                if (received == 0)
                    continue; 

                totalReceived += received;
                if (totalReceived < buffer.Length)
                    break;
                
                Array.Resize(ref buffer, buffer.Length + 512);
            } 
            while (socketUser.UnderSocket.Available > 0);
            
            for (int totalRead = 0; totalReceived - totalRead > 0;)
            {
                int length = ByteUtils.Byte2Int(buffer, totalRead);

                string rawMessage = Encoding.UTF8.GetString(buffer, totalRead + 2, length);
                totalRead += length + 2;

                if (!JsonHelper.TryDeserialize<WebSocketMessage>(rawMessage, out var socketMessage))
                    continue;
                
                if(totalReceived - totalRead is <= 0)
                    ArrayPool.Return(buffer, true);
                
                switch (socketMessage.WebSocketOpCode)
                {
                    case WebSocketOpcodes.Login when !socketUser.IsIdentified:
                    {
                        if (String.IsNullOrWhiteSpace(socketMessage.Message))
                            continue;

                        if (!JsonHelper.TryDeserialize<CreateAccountRequest>(socketMessage.Message, out var createAccountRequest))
                            break;

                        await using var context = await _dbContext.CreateDbContextAsync();
                        var (user, errorMessage) = await context.LogUserIn(createAccountRequest);
                        if (errorMessage is not null)
                        {
                            await socketUser.Send(RedisEventTypes.Error, RestErrors.InvalidUserOrPass);
                            continue;
                        }

                        user!.SessionId = sessionId;
                        socketUser.IsIdentified = true;
                        await context.SaveChangesAsync();

                        List<Guid>? channelIds = user.Channels.Select(x => x.Id).ToList();
                        foreach (var channelId in channelIds)
                            await RedisUserCache.CacheChannelUser(channelId.ToString(), sessionId.ToString());

                        await socketUser.Send(WebSocketOpcodes.Login, JsonSerializer.Serialize(channelIds));
                        continue;
                    }

                    case WebSocketOpcodes.Logout when socketUser.IsIdentified:
                    {
                        await using var context = await _dbContext.CreateDbContextAsync();
                        await context.LogoutUser(sessionId);

                        socketUser.UserCancellation.Cancel();
                        return;
                    }

                    case WebSocketOpcodes.Heartbeat when socketUser.IsIdentified:
                    {
                        receivedAck = true;
                        continue;
                    }
                }
            }
        }
        
        socketUser.Dispose();
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