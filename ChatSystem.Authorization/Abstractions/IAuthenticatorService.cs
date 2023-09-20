using ChatSystem.Authorization.Models;

namespace ChatSystem.Authorization.Abstractions;

public interface IAuthenticatorService
{
    ChatSystemAuthenticated GenerateToken(JwtUser user);
}