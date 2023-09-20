using ChatSystem.Data.Models;
using Mapster;

namespace ChatSystem.Logic.Models.Rest;

[AdaptTo(typeof(ChatUser))]
public record CreateAccountRequest(string Username, string Email, string Password)
{
    public static explicit operator ChatUser(CreateAccountRequest request) =>
        request.Adapt<ChatUser>();
};
