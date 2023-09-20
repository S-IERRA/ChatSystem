namespace ChatSystem.ApiWrapper.Models.Response;

public record BasicChatServer
{
    public Guid Id { get; set; }

    public BasicChatServerOwner ServerOwner { get; set; }
    
    public required string Name { get; set; }
}

public class BasicChatServerOwner
{
    public BasicChatUser User { get; set; }
}

public class BasicChatServerInvite
{
    public Guid Id { get; set; }

    public required string InviteCode { get; set; }
}

public class BasicChatServerLog
{
    public Guid Id { get; set; }
    
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    public required string LogMessage { get; set; }
}