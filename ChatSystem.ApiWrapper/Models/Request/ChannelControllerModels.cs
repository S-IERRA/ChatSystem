namespace ChatSystem.ApiWrapper.Models;

public record CreateServerChannelRequest(string Name, List<Guid> ViewPermissions);