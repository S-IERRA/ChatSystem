using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using ChatSystem.Authorization.Abstractions;
using ChatSystem.Authorization.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ChatSystem.Authorization.Logic;

public class Authenticate : IAuthenticatorService
{
    private readonly JwtConfig _jwtConfig;
    
    public Authenticate(IOptions<JwtConfig> jwtConfig)
    {
        _jwtConfig = jwtConfig.Value;
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        
        return Convert.ToBase64String(randomNumber);
    }
    
    public ChatSystemAuthenticated GenerateToken(JwtUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
        var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _jwtConfig.Issuer,
            _jwtConfig.Audience,
            user.Claims(),
            expires: DateTime.UtcNow.AddMinutes(120),
            signingCredentials: signIn);

        string jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
        string refreshToken = GenerateRefreshToken();

        return new ChatSystemAuthenticated(jwtToken, refreshToken);
    }
}