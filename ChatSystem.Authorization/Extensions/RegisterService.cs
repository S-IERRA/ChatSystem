using System.Text;
using ChatSystem.Authorization.Abstractions;
using ChatSystem.Authorization.Logic;
using ChatSystem.Authorization.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ChatSystem.Authorization.Extensions;

public enum ChatSystemAuthPolicy
{
    Admin
}

public static class RegisterService
{
    public static void RegisterAuthorization(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var jwtConfig = configuration.GetSection("Jwt").Get<JwtConfig>()!;
        serviceCollection.Configure<JwtConfig>(configuration.GetSection("Jwt"));

        serviceCollection.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {   
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = jwtConfig.Audience,
                    ValidIssuer = jwtConfig.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key))
                };
                
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["JwtToken"];

                        return Task.CompletedTask;
                    }
                };
            });

        serviceCollection.AddScoped(typeof(IAuthenticatorService), typeof(Authenticate));
    }

    public static void RegisterAuthorizationMiddlewares(this IApplicationBuilder applicationBuilder)
    {
       // applicationBuilder.UseMiddleware<CustomJwtMiddleware>();
        applicationBuilder.UseMiddleware<TokenInvalidationMiddleware>();
        
        applicationBuilder.UseWhen(
            context => context.Request.Path.StartsWithSegments("/admin/api"),
            builder =>
            {
                builder.UseMiddleware<AdminRoleCheckMiddleware>();
            });
    }
}   