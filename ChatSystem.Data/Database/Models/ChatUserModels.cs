using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatSystem.Data.Models;

public record ChatUser : IEntityTypeConfiguration<ChatUser>
{
    public Guid Id { get; set; }
    
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string HashedPassword { get; set; }
    
    public Guid? SessionId { get; set; }

    public Guid? RegistrationToken { get; set; }
    public Guid? PasswordResetToken { get; set; }
    
    public ICollection<ChatChannel> Channels { get; set; } = new HashSet<ChatChannel>();
    public ICollection<ChatRelationship> Relationships { get; set; } = new HashSet<ChatRelationship>();

    public void Configure(EntityTypeBuilder<ChatUser> builder)
    {
        builder.Property(x => x.Id)
            .HasDefaultValue(Guid.NewGuid());
    }
}


public record ChatRelationship : IEntityTypeConfiguration<ChatRelationship>
{
    public Guid Id { get; set; }
    
    public required Guid CreatorId { get; set; }
    public ICollection<ChatUser> Users { get; set; } = new HashSet<ChatUser>();

    public required ChatRelationShipType Type { get; set; } = ChatRelationShipType.None;

    public void Configure(EntityTypeBuilder<ChatRelationship> builder)
    {
        builder.Property(x => x.Id)
            .HasDefaultValue(Guid.NewGuid());

        builder.HasMany(x => x.Users)
            .WithMany(x => x.Relationships);
    }
}

public enum ChatRelationShipType
{
    None,
    Blocked,
    Friends,
    Outgoing,
    Incoming
}