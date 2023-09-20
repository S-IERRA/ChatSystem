using ChatSystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatSystem.Data;

public sealed class EntityFrameworkContext : DbContext
{
    public EntityFrameworkContext(DbContextOptions options) : base(options)
    {
        Database.EnsureCreated();
    }
    
    public DbSet<ChatUser> Users { get; set; }
    public DbSet<ChatRelationship> Relationships { get; set; }

    public DbSet<ChatChannel> Channels { get; set; }
    public DbSet<ChatMessage> Messages { get; set; }

    public DbSet<ChatServer> Servers { get; set; }
    public DbSet<ChatServerLog> ServerLogs { get; set; }
    public DbSet<ChatServerRole> ServerRoles { get; set; }
    public DbSet<ChatServerInvite> ServerInvites { get; set; }
    public DbSet<ChatServerMember> ServerMembers { get; set; }
    public DbSet<ChatServerBannedUser> ServerBannedUsers { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}