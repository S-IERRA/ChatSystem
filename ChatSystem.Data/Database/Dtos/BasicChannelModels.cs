using ChatSystem.Data.Models;
using Mapster;

namespace ChatSystem.Data.Dtos;

[AdaptFrom(typeof(ChatChannel))]
public class BasicGroupChannel
{
    public Guid Id { get; set; }

    public required Guid ModeratorId { get; set; }
    public required ForeignBasicChatUser Moderator { get; set; }

    public required string Name { get; set; }
    public required ChatChannelType Type = ChatChannelType.Group;

    public virtual ICollection<ForeignBasicChatUser> Users { get; set; } = new HashSet<ForeignBasicChatUser>();
    
    public static explicit operator BasicGroupChannel(ChatChannel request) =>
        request.Adapt<BasicGroupChannel>();
}

[AdaptFrom(typeof(ChatChannel))]
public class BasicDmChannel
{
    public Guid Id { get; set; }
    
    public required string Name { get; set; }
    public required ChatChannelType Type = ChatChannelType.Dm;

    public virtual ICollection<ForeignBasicChatUser> Users { get; set; } = new HashSet<ForeignBasicChatUser>();
    
    public static explicit operator BasicDmChannel(ChatChannel request) =>
        request.Adapt<BasicDmChannel>();
}

[AdaptFrom(typeof(ChatChannel))]
public class BasicServerChannel
{
    public Guid Id { get; set; }
    
    public required Guid? ServerId { get; set; }
    public required BasicChatServer? Server { get; set; }
    
    public required string Name { get; set; }
    public required ChatChannelType Type = ChatChannelType.Server;

    //ToDo: needs basic type
    public virtual ICollection<ChatServerRole> ViewPermissions { get; set; } = new HashSet<ChatServerRole>();  

    public static explicit operator BasicServerChannel(ChatChannel request) =>
        request.Adapt<BasicServerChannel>();
}