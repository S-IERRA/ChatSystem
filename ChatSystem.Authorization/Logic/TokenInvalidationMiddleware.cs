using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace ChatSystem.Authorization.Logic;

public class TokenInvalidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _memoryCache;

    public TokenInvalidationMiddleware(RequestDelegate next, IMemoryCache memoryCache)
    {
        _next = next;
        _memoryCache = memoryCache;
    }

    public async Task Invoke(HttpContext context)
    {
        string? token = context.Request.Cookies["JwtToken"];

        if (string.IsNullOrEmpty(token))
        {
            await _next(context);
            return;
        }
        
        if (_memoryCache.TryGetValue(token, out _))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }
}
