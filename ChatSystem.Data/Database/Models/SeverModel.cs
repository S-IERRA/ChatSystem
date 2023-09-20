using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatSystem.Data.Models;

[Flags]
public enum ChatPermissions
{
    Member,
    CanKick,
    CanBan,
    CanViewLogs,
    RoleControl,
    CanCreateInvite,
    CanCreateChannels,
}

public record ChatServer : IEntityTypeConfiguration<ChatServer>
{
    public Guid Id { get; set; }

    public required Guid OwnerId { get; set; }
    public ChatUser Owner { get; set; }
    
    public required string Name { get; set; }

    public virtual ICollection<ChatChannel> Channels { get; set; } = new HashSet<ChatChannel>();
    public virtual ICollection<ChatServerLog> Logs { get; set; } =  new HashSet<ChatServerLog>();
    public virtual ICollection<ChatServerRole> Roles { get; set; } =  new HashSet<ChatServerRole>();
    public virtual ICollection<ChatServerInvite> Invites { get; set; } =  new HashSet<ChatServerInvite>();
    public virtual ICollection<ChatServerMember> Members { get; set; } =  new HashSet<ChatServerMember>();
    public virtual ICollection<ChatServerBannedUser> BannedUsers { get; set; } =  new HashSet<ChatServerBannedUser>();

    public void Configure(EntityTypeBuilder<ChatServer> builder)
    {
        builder.Property(x => x.Id)
            .HasDefaultValue(Guid.NewGuid());
        
        builder.HasOne(e => e.Owner)
            .WithMany()
            .HasForeignKey(e => e.OwnerId)
            .IsRequired();
    }
}

public record ChatServerInvite : IEntityTypeConfiguration<ChatServerInvite>
{
    public Guid Id { get; set; }
    
    public required string InviteCode { get; set; }
    
    public required Guid ServerId { get; set; }
    public ChatServer Server { get; set; }
    
    public void Configure(EntityTypeBuilder<ChatServerInvite> builder)
    {
        builder.Property(x => x.Id)
            .HasDefaultValue(Guid.NewGuid());

        builder.HasOne(x => x.Server)
            .WithMany(x => x.Invites)
            .HasForeignKey(x => x.ServerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public record ChatServerLog : IEntityTypeConfiguration<ChatServerLog>
{
    public Guid Id { get; set; }
    
    public required Guid ServerId { get; set; }
    public ChatServer Server { get; set; }
    
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    public required string LogMessage { get; set; }
    
    public void Configure(EntityTypeBuilder<ChatServerLog> builder)
    {
        builder.HasOne(x => x.Server)
            .WithMany(x => x.Logs)
            .HasForeignKey(x => x.ServerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public record ChatServerRole : IEntityTypeConfiguration<ChatServerRole>
{
    public Guid Id { get; set; }
    
    public required string Name { get; set; }
    
    public required Guid ServerId { get; set; }
    public ChatServer Server { get; set; }
    
    //Do not return if user has no perms
    public virtual ICollection<ChatChannel> Channels { get; set; } =  new HashSet<ChatChannel>();
    public virtual ICollection<ChatServerMember> Members { get; set; } =  new HashSet<ChatServerMember>();
    
    public void Configure(EntityTypeBuilder<ChatServerRole> builder)
    {
        builder.ToTable("ServerRoles");

        builder.Property(x => x.Id)
            .HasDefaultValue(Guid.NewGuid());

        builder.HasOne(x => x.Server)
            .WithMany(x => x.Roles)
            .HasForeignKey(x => x.ServerId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasMany(x => x.Members)
            .WithMany(x => x.Roles);
    }
}

public class ChatServerMember : IEntityTypeConfiguration<ChatServerMember>
{
    public required Guid UserId { get; set; }
    public ChatUser User { get; set; }

    public required Guid ServerId { get; set; }
    public ChatServer Server { get; set; }

    public ChatPermissions Permissions { get; set; } = ChatPermissions.Member;

    public virtual ICollection<ChatServerRole> Roles { get; set; } = new HashSet<ChatServerRole>();

    public void Configure(EntityTypeBuilder<ChatServerMember> builder)
    {
        builder.ToTable("ServerMembers");

        builder.HasKey(x => new { x.UserId, x.ServerId });

        builder.HasOne(x => x.Server)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.ServerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

[AdaptFrom(typeof(ChatServerMember))]
public class ChatServerBannedUser : IEntityTypeConfiguration<ChatServerBannedUser>
{
    public Guid UserId { get; set; }
    public ChatUser User { get; set; }

    public Guid ServerId { get; set; }
    public ChatServer Server { get; set; }

    public DateTimeOffset BanDate { get; set; } = DateTimeOffset.UtcNow;
    public string BanReason { get; set; }
    
    public static implicit operator ChatServerBannedUser(ChatServerMember member)
        => member.Adapt<ChatServerBannedUser>();

    public void Configure(EntityTypeBuilder<ChatServerBannedUser> builder)
    {
        builder.HasKey(x => new { x.UserId, x.ServerId });

        builder.HasOne(x => x.Server)
            .WithMany(x => x.BannedUsers)
            .HasForeignKey(x => x.ServerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}