using System.Net;
using ChatSystem.Logic.Models.Websocket;

namespace ChatSystem.Logic.Abstractions;

public interface IRedisCommunicationService
{
    Task SendViaChannelAsync(RedisEventTypes type, Guid channelId, string message);
    Task SendViaChannelAsync(RedisOpcodes opCode, Guid channelId, string message = "");

    Task SendViaSessionAsync(RedisEventTypes type, Guid sessionId, string message);
    Task SendViaSessionAsync(RedisOpcodes opCode, Guid sessionId, string message = "");
}