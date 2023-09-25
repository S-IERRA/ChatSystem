using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatSystem.Data.Models;

public enum ChatChannelType
{
    Unknown,
    Dm,
    Group,
    Server
}

public class ChatChannel : IEntityTypeConfiguration<ChatChannel>
{
    public Guid Id { get; set; }

    //if type isn't dm / group, then this will be null
    public Guid? ModeratorId { get; set; }
    public ChatUser? Moderator { get; set; }
    
    //if type isn't server, then this will be null
    public Guid? ServerId { get; set; }
    public ChatServer? Server { get; set; }

    public required string Name { get; set; }
    public required ChatChannelType Type { get; set; } = ChatChannelType.Unknown;

    public virtual ICollection<ChatUser> Users { get; set; } = new HashSet<ChatUser>();
    public virtual ICollection<ChatMessage> Messages { get; set; } = new HashSet<ChatMessage>();  

    //if type isn't server, then this will be null, do not return this in-case of non server channels, can have a generic type for mod and non mod users to view server channels that would work
    public virtual ICollection<ChatServerRole> ViewPermissions { get; set; } = new HashSet<ChatServerRole>();  
    
    public void Configure(EntityTypeBuilder<ChatChannel> builder)
    {
        builder.Property(x => x.Id)
            .HasDefaultValue(Guid.NewGuid());

        builder.HasOne(x => x.Moderator)
            .WithMany();

        builder.HasMany(x => x.Users)
            .WithMany(x => x.Channels);
        
        builder.HasMany(x => x.ViewPermissions)
            .WithMany(x => x.Channels);

        builder.HasOne(x => x.Server)
            .WithMany(x => x.Channels)
            .HasForeignKey(x => x.ServerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}