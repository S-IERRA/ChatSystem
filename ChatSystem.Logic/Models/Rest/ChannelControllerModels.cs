using ChatSystem.Data.Models;
using Mapster;

namespace ChatSystem.Logic.Models.Rest;

[AdaptTo(typeof(ChatChannel))]
public record CreateServerChannelRequest(string Name, List<Guid> ViewPermissions)
{
    public const ChatChannelType Type = ChatChannelType.Server;

    public static explicit operator ChatChannel(CreateServerChannelRequest request) =>
        request.Adapt<ChatChannel>();
};
