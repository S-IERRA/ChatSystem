using System.Text.Json;
using ChatSystem.Data.Caching;
using ChatSystem.Logic.Abstractions;
using ChatSystem.Logic.Helpers;
using ChatSystem.Logic.Models.Websocket;
using StackExchange.Redis;

namespace ChatSystem.Logic.Websocket;

public class RedisCommunicationImplementation : IRedisCommunicationService
{
    private static readonly ConnectionMultiplexer CacheClient = RedisConnectionManager.Connection;
    private static readonly ISubscriber RedisSubscriber = CacheClient.GetSubscriber();
    
    private const string GeneralChannel = "general";

    public RedisCommunicationImplementation()
    {
        ChannelMessageQueue messageQueue = RedisSubscriber.Subscribe(GeneralChannel);
        messageQueue.OnMessage(async message =>
        {
            if (!JsonHelper.TryDeserialize<RedisTransferMessage>(message.Message, out var transferMessage))
            {
                await RedisSubscriber.PublishAsync(GeneralChannel, message.ToString());
                return;
            }

            switch (transferMessage.OpCodes)
            {
                case RedisOpcodes.Login:
                    //ToDo: handle client authentication and create a heartbeat for the dll and ui
                    break;
                
                //ToDo: Delete user session
                case RedisOpcodes.Logout:
                    break;
            }
        });
    }

    private async Task SendAsync(RedisOpcodes opCode, RedisEventTypes eventType, Guid sessionId, string message)
    {
        var transferMessage = new RedisTransferMessage(opCode, eventType, sessionId, message);
        string serializedData = JsonSerializer.Serialize(transferMessage);

        await RedisSubscriber.PublishAsync(GeneralChannel, serializedData);
    }

    public async Task SendAsync(RedisEventTypes type, Guid sessionId, string message) =>
        await SendAsync(RedisOpcodes.Event, type, sessionId, message);

    public async Task SendAsync(RedisOpcodes opCode, Guid sessionId, string message = "") =>
        await SendAsync(opCode, RedisEventTypes.None, sessionId, message);
}