using System.Text.Json;
using ChatSystem.Data.Caching;
using ChatSystem.Logic.Helpers;
using ChatSystem.Logic.Models.Websocket;
using StackExchange.Redis;

namespace ChatSystem.Websocket.Logic;

//ToDO: Move this to the logic layer, handle the switch of opcodes in a abstract method
//ToDo: Set the client & cache on the SocketUser when receives login opcode, pass to logic layer
public class RedisCommunicator
{
    private static readonly ConnectionMultiplexer CacheClient = RedisConnectionManager.Connection;
    private static readonly ISubscriber RedisSubscriber = CacheClient.GetSubscriber();
    
    private const string GeneralChannel = "general";

    //ToDo: add replies
    public RedisCommunicator()
    {
        ChannelMessageQueue messageQueue = RedisSubscriber.Subscribe(GeneralChannel);
        messageQueue.OnMessage(message =>
        {
            if (!JsonHelper.TryDeserialize<RedisTransferMessage>(message.Message, out var transferMessage))
                return;

            Console.WriteLine(transferMessage.ToString());
            
            if (transferMessage.OpCodes is not RedisOpcodes.Event)
                return;
            
            //ToDo: fetch from cache
            /*SocketUser socket = ClientList.FirstOrDefault(x => x.Key == transferMessage.SessionId).Value; 
            _ = socket.Send(WebSocketOpCodes.Event, transferMessage.Data);*/
            
            switch (transferMessage.EventType)
            {
                case RedisEventTypes.None:
                    break;
                case RedisEventTypes.NewMessage:
                    break;
                case RedisEventTypes.EditMessage:
                    break;
                case RedisEventTypes.DeleteMessage:
                    break;
                case RedisEventTypes.IncomingFriendRequest:
                    break;
                case RedisEventTypes.OutgoingFriendRequest:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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