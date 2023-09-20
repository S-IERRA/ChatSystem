using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using ChatSystem.ApiWrapper.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatSystem.Data.Models;

public class ChatMessage : IEntityTypeConfiguration<ChatMessage>
{
    public Guid Id { get; set; }
    
    public required Guid ChannelId { get; set; }
    public ChatChannel Channel { get; set; }
    
    public required Guid AuthorId { get; set; }
    public ChatUser Author { get; set; }
    
    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;
    public required string Content { get; set; }

    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.Property(x => x.Id)
            .HasDefaultValue(Guid.NewGuid());

        builder.HasOne(x => x.Channel)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorId);
    }
    
    public static implicit operator string(ChatMessage message) => JsonSerializer.Serialize(message, JsonHelper.JsonSerializerOptions);
}