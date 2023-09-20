using System.IdentityModel.Tokens.Jwt;
using ChatSystem.Authorization.Models;
using ChatSystem.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ChatSystem.Authorization.Logic;

public class AdminRoleCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDbContextFactory<EntityFrameworkContext> _dbContext;

    public AdminRoleCheckMiddleware(RequestDelegate next, IDbContextFactory<EntityFrameworkContext> dbContext)
    {
        _next = next;
        _dbContext = dbContext;
    }

    public async Task Invoke(HttpContext context)
    {
        string? token = context.Request.Cookies["JwtToken"];

        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var jwtHandler = new JwtSecurityTokenHandler();
        JwtSecurityToken jwtToken = jwtHandler.ReadJwtToken(token);
        
        string? chatUserId = jwtToken.Claims.FirstOrDefault(c => c.Type == ChatClaims.UserId)?.Value;
        if(chatUserId is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        
        await using var dbContext = await _dbContext.CreateDbContextAsync();
        if(await dbContext.Users.FirstOrDefaultAsync(x=>x.Id == Guid.Parse(chatUserId)) is not { } chatUser)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        
        /*ToDo: if(!chatUser.Roles.HasFlag(NumixRole.Administrator))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }*/
        
        await _next(context);
    }
}