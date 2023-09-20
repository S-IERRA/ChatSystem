using ChatSystem.Logic.Models.Websocket;

namespace ChatSystem.Logic.Abstractions;

public interface IRedisCommunicationService
{
    Task SendAsync(RedisEventTypes type, Guid sessionId, string message);
    Task SendAsync(RedisOpcodes opCode, Guid sessionId, string message = "");
}