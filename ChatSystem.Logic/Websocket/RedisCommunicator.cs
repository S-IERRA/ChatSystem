using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using ChatSystem.Data;
using ChatSystem.Data.Caching;
using ChatSystem.Data.Models;
using ChatSystem.Logic.Abstractions;
using ChatSystem.Logic.ChatSystem_Logic;
using ChatSystem.Logic.Constants;
using ChatSystem.Logic.Helpers;
using ChatSystem.Logic.Models;
using ChatSystem.Logic.Models.Rest;
using ChatSystem.Logic.Models.Websocket;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ChatSystem.Logic.Websocket;

public class RedisCommunicationImplementation : IRedisCommunicationService
{
    private static readonly ConnectionMultiplexer CacheClient = RedisConnectionManager.Connection;
    private static readonly ISubscriber RedisSubscriber = CacheClient.GetSubscriber();
    
    private readonly IDbContextFactory<EntityFrameworkContext> _dbContext;

    private const string GeneralChannel = "general";
    
    public RedisCommunicationImplementation(IDbContextFactory<EntityFrameworkContext> dbContext)
    {
        _dbContext = dbContext;
        
        ChannelMessageQueue messageQueue = RedisSubscriber.Subscribe(GeneralChannel);
        messageQueue.OnMessage(async message =>
        {
            if (!JsonHelper.TryDeserialize<RedisTransferMessage>(message.Message, out var transferMessage))
            {
                await RedisSubscriber.PublishAsync(GeneralChannel, message.ToString());
                return;
            }
            
            Console.WriteLine(transferMessage.ToString());
        });
    }

    private async Task SendViaChannelAsync(RedisOpcodes opCode, RedisEventTypes eventType, Guid channelId, string message)
    {
        List<string> sessionIds = await RedisUserCache.GetChannelUsers(channelId);
        foreach (var sessionId in sessionIds)
        {
            if(!Guid.TryParse(sessionId, out var sessionGuid))
                continue;
            
            var transferMessage = new RedisTransferMessage(opCode, eventType, sessionGuid, message);
            string serializedData = JsonSerializer.Serialize(transferMessage, JsonHelper.JsonSerializerOptions);

            Console.WriteLine(sessionId);

            await RedisSubscriber.PublishAsync(sessionId, serializedData);   
        }
    }

    public async Task SendViaChannelAsync(RedisEventTypes type, Guid channelId, string message) =>
        await SendViaChannelAsync(RedisOpcodes.Event, type, channelId, message);

    public async Task SendViaChannelAsync(RedisOpcodes opCode, Guid channelId, string message = "") =>
        await SendViaChannelAsync(opCode, RedisEventTypes.None, channelId, message);
    
    private async Task SendViaSessionAsync(RedisOpcodes opCode, RedisEventTypes eventType, Guid sessionId, string message)
    {
        var transferMessage = new RedisTransferMessage(opCode, eventType, sessionId, message);
        string serializedData = JsonSerializer.Serialize(transferMessage, JsonHelper.JsonSerializerOptions);
        
        await RedisSubscriber.PublishAsync(sessionId.ToString(), serializedData);
    }

    public async Task SendViaSessionAsync(RedisEventTypes type, Guid sessionId, string message) =>
        await SendViaChannelAsync(RedisOpcodes.Event, type, sessionId, message);

    public async Task SendViaSessionAsync(RedisOpcodes opCode, Guid sessionId, string message = "") =>
        await SendViaChannelAsync(opCode, RedisEventTypes.None, sessionId, message);
    
    public static IPEndPoint? CreateIpEndPoint(string ipAddressString, int port)
    {
        if (!IPAddress.TryParse(ipAddressString, out var ipAddress))
            return null;
        
        return ipAddress.AddressFamily == AddressFamily.InterNetwork 
            ? new IPEndPoint(ipAddress, port)
            : null;
    }
}