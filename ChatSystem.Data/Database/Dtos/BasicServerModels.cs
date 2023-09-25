using ChatSystem.Data.Models;
using Mapster;

namespace ChatSystem.Data.Dtos;

[AdaptFrom(typeof(ChatServer))]
public record BasicChatServer
{
    public Guid Id { get; set; }

    public BasicChatServerOwner ServerOwner { get; set; }
    
    public required string Name { get; set; }
    
    public static explicit operator BasicChatServer(ChatServer request) =>
        request.Adapt<BasicChatServer>();
}

public record BasicChatServerOwner
{
    public BasicChatUser User { get; set; }
}

[AdaptFrom(typeof(ChatServerInvite))]
public record BasicChatServerInvite
{
    public Guid Id { get; set; }

    public required string InviteCode { get; set; }
    
    public static explicit operator BasicChatServerInvite(ChatServerInvite request) =>
        request.Adapt<BasicChatServerInvite>();
}

[AdaptFrom(typeof(ChatServerLog))]
public record BasicChatServerLog
{
    public Guid Id { get; set; }
    
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    public required string LogMessage { get; set; }
    
    public static explicit operator BasicChatServerLog(ChatServerLog request) =>
        request.Adapt<BasicChatServerLog>();
}