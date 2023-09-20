namespace ChatSystem.Logic.Models.Websocket;

public record WebSocketMessage(WebSocketOpcodes WebSocketOpCode, string? Message, WebSocketEvents? EventType, Guid? Session);

public enum WebSocketOpcodes
{
    Event  = 0,
    Login  = 1,
    Logout = 2,
    Heartbeat,
}

public enum WebSocketEvents
{
    Message,
    
    FriendIncoming,
    FriendAccept,
    FriendDecline,
    
    Block
}