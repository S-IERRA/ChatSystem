namespace ChatSystem.ApiWrapper.Models.Response;

public enum ChatChannelType
{
    Unknown,
    Dm,
    Group,
    Server
}

public class BasicGroupChannel
{
    public Guid Id { get; set; }

    public required Guid ModeratorId { get; set; }
    public required ForeignBasicChatUser Moderator { get; set; }

    public required string Name { get; set; }
    public required ChatChannelType Type = ChatChannelType.Group;

    public virtual ICollection<ForeignBasicChatUser> Users { get; set; } = new HashSet<ForeignBasicChatUser>();
}

public class BasicDmChannel
{
    public Guid Id { get; set; }
    
    public required string Name { get; set; }
    public required ChatChannelType Type = ChatChannelType.Dm;

    public virtual ICollection<ForeignBasicChatUser> Users { get; set; } = new HashSet<ForeignBasicChatUser>();
}

public class BasicServerChannel
{
    public Guid Id { get; set; }
    
    public required Guid? ServerId { get; set; }
    public required BasicChatServer? Server { get; set; }
    
    public required string Name { get; set; }
    public required ChatChannelType Type = ChatChannelType.Server;

    //public virtual ICollection<ChatServerRole> ViewPermissions { get; set; } = new HashSet<ChatServerRole>();
}