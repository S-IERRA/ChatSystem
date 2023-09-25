using System.Text.Json;
using ChatSystem.ApiWrapper.Helpers;
using ChatSystem.Data.Dtos;
using ChatSystem.Data.Models;
using Mapster;

namespace ChatSystem.Data.Dtos;

[AdaptFrom(typeof(ChatMessage))]
public record BasicMessage
{
    public Guid Id { get; set; }
    
    public BasicChannel Channel { get; set; }
    
    public ForeignBasicChatUser Author { get; set; }
    
    public DateTimeOffset CreationDate { get; set; } 
    public required string Content { get; set; }
    
    public static explicit operator BasicMessage(ChatMessage request) =>
        request.Adapt<BasicMessage>();
    
    public static implicit operator string(BasicMessage message) => JsonSerializer.Serialize(message, JsonHelper.JsonSerializerOptions);
}