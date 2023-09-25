namespace ChatSystem.Logic.Models.Websocket;

public record WebSocketMessage(WebSocketOpcodes WebSocketOpCode, string? Message, RedisEventTypes? EventType, Guid? Session);

public enum WebSocketOpcodes
{
    Event  = 0,
    Login  = 1,
    Logout = 2,
    Heartbeat,
}