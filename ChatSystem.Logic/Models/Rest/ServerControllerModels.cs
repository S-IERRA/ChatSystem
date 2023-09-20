using ChatSystem.Data.Models;
using Mapster;

namespace ChatSystem.Logic.Models.Rest;

[AdaptTo(typeof(ChatServer))]
public record CreateServerRequest(string Name)
{
    public static explicit operator ChatServer(CreateServerRequest request) =>
        request.Adapt<ChatServer>();
};

[AdaptTo(typeof(ChatServerRole))]
public record CreateRoleRequest(string Name)
{
    public static explicit operator ChatServerRole(CreateRoleRequest request) =>
        request.Adapt<ChatServerRole>();
};