using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ChatSystem.Authorization.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ChatSystem.Authorization.Logic;

public class CustomJwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JwtConfig _jwtConfig;

    public CustomJwtMiddleware(RequestDelegate next, IOptions<JwtConfig> jwtConfig)
    {
        _next = next;
        _jwtConfig = jwtConfig.Value;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!ShouldApplyJwtValidation(context))
        {
            await _next(context);
            return;
        }

        var token = context.Request.Cookies["JwtToken"];

        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = 401;
            return;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _jwtConfig.Issuer,
                ValidAudience = _jwtConfig.Audience,
                IssuerSigningKey = key
            }, out SecurityToken validatedToken);

            await _next(context);
        }
        catch (Exception)
        {
            context.Response.StatusCode = 401;
        }
    }

    private bool ShouldApplyJwtValidation(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        var authorizeAttribute = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>();
        return authorizeAttribute != null;
    }
}