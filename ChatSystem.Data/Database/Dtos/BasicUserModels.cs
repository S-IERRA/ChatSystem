using ChatSystem.Data.Models;
using Mapster;

namespace ChatSystem.Data.Dtos;

[AdaptFrom(typeof(ChatUser))]
public record BasicChatUser
{
    public Guid Id { get; set; }

    public required string Username { get; set; }
    public required string Email { get; set; }
    
    public static explicit operator BasicChatUser(ChatUser fullUser) =>
        fullUser.Adapt<BasicChatUser>();
}

[AdaptFrom(typeof(ChatUser))]
public record ForeignBasicChatUser
{
    public Guid Id { get; set; }

    public required string Username { get; set; }
    
    public static explicit operator ForeignBasicChatUser(ChatUser fullUser) =>
        fullUser.Adapt<ForeignBasicChatUser>();
}