using System.Globalization;
using System.Net;
using System.Security.Claims;
using ChatSystem.Data.Models;
using Mapster;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ChatSystem.Authorization.Models;

public class ChatClaims
{
    public const string UserId = "UserId";
    public const string SessionId = "SessionId";

    public const string UserEmail = "UserEmail";
    public const string Roles = "Roles";
    public const string IssuingIpAddress = "IssuingIpAddress";
}

[AdaptFrom(typeof(ChatUser))]
public class JwtUser
{
    public Guid UserId { get; set; }

    public required Guid SessionId { get; set; }

    public required IPAddress? IssuingIpAddress { get; set; }
    public required string Email { get; set; }

    //ToDo: public NumixRole Roles { get; set; }
    
    public IEnumerable<Claim> Claims()
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, "JWTServiceAccessToken"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
            
            new Claim(ChatClaims.UserId, UserId.ToString()),
            new Claim(ChatClaims.UserEmail, Email),
            
            new Claim(ChatClaims.SessionId, SessionId.ToString()),
            
            new Claim(ChatClaims.IssuingIpAddress, IssuingIpAddress?.ToString() ?? ""),

            // new Claim(NumixClaims.Roles, Roles.ToString())
        };
        
        return claims;
    }

    private static readonly TypeAdapterConfig Config = new TypeAdapterConfig()
        .NewConfig<ChatUser, JwtUser>()
        .Map(dest => dest.UserId, src => src.Id)
        .Config;
    
    public static explicit operator JwtUser(ChatUser user) =>
        user.Adapt<JwtUser>(Config);
}