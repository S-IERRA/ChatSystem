namespace ChatSystem.Authorization.Models;

public record ChatSystemAuthenticated(string JwtToken, string? RefreshToken);