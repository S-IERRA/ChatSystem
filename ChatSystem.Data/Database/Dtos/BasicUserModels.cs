using System.Text.Json;
using ChatSystem.ApiWrapper.Helpers;
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


[AdaptFrom(typeof(ChatRelationship))]
public record BasicChatRelationship
{
    public Guid Id { get; set; }
    
    public ICollection<ForeignBasicChatUser> Users { get; set; } = new HashSet<ForeignBasicChatUser>();

    public required ChatRelationShipType Type { get; set; } = ChatRelationShipType.None;
    
    public static explicit operator BasicChatRelationship(ChatRelationship fullUser) =>
        fullUser.Adapt<BasicChatRelationship>();
    
    public static implicit operator string(BasicChatRelationship message) => JsonSerializer.Serialize(message, JsonHelper.JsonSerializerOptions);
}

//ToDo: type could use a bit of expanding with foreign users
[AdaptFrom(typeof(ChatServerRole))]
public record BasicChatServerRole
{
    public Guid Id { get; set; }

    public required string Name { get; set; }
}