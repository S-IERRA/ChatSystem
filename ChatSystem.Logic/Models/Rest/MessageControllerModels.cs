using ChatSystem.Data.Models;
using Mapster;

namespace ChatSystem.Logic.Models.Rest;

[AdaptTo(typeof(ChatMessage))]
public record CreateMessageRequest(string Content)
{
    public static explicit operator ChatMessage(CreateMessageRequest request) =>
        request.Adapt<ChatMessage>();
};
