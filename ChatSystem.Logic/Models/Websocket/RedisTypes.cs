namespace ChatSystem.Logic.Models.Websocket;

public record RedisTransferMessage(RedisOpcodes OpCodes, RedisEventTypes EventType, Guid SessionId, string Data);

public enum RedisOpcodes
{
    Event  = 0,
    Login  = 1,
    Logout = 2,
}

public enum RedisEventTypes
{
    None,
    NewMessage,
    EditMessage,
    DeleteMessage,
    
    IncomingFriendRequest,
    OutgoingFriendRequest
}