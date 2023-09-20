namespace ChatSystem.ApiWrapper.Models.Response;

public record BasicChatUser
{
    public Guid Id { get; set; }

    public required string Username { get; set; }
    public required string Email { get; set; }
}

public record ForeignBasicChatUser
{
    public Guid Id { get; set; }

    public required string Username { get; set; }
}