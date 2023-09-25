using ChatSystem.Data;
using ChatSystem.Data.Models;
using ChatSystem.Logic.ChatSystem_Logic.Algorithms;
using ChatSystem.Logic.Constants;
using ChatSystem.Logic.Models;
using ChatSystem.Logic.Models.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ILogger = Serilog.ILogger;

namespace ChatSystem.Logic.ChatSystem_Logic;

public static class AccountManager
{
    private static readonly Random Random = new Random();

    public static async Task<ChatUser> CreateUserAccount(this EntityFrameworkContext context, CreateAccountRequest userRequest)
    {
        var newUser = (ChatUser)userRequest;
        newUser.RegistrationToken = Guid.NewGuid();
        newUser.HashedPassword = Pbkdf2.CreateHash(userRequest.Password);
        
        EntityEntry<ChatUser> dbEntity = context.Users.Add(newUser);

        await context.SaveChangesAsync();

        return dbEntity.Entity;
    }

    public static async Task<GenericResponse<ChatUser>> LogUserIn(this EntityFrameworkContext context, CreateAccountRequest userRequest)
    {
        if (await context.Users.Where(x => x.Email == userRequest.Email)
                .Include(x=>x.Channels)
                .FirstOrDefaultAsync() is { } chatUser)
        {
            return !Pbkdf2.ValidatePassword(userRequest.Password, chatUser.HashedPassword)
                ? new GenericResponse<ChatUser>(null, RestErrors.InvalidUserOrPass)
                : new GenericResponse<ChatUser>(chatUser, null);
        }

        await Task.Delay(Random.Next(100, 500));
        return new GenericResponse<ChatUser>(null, RestErrors.InvalidUserOrPass);
    }
    
    public static async Task<GenericResponse<ChatUser>> LogoutUser(this EntityFrameworkContext context, Guid sessionId)
    {
        if (await context.Users.FirstOrDefaultAsync(x => x.SessionId == sessionId) is not { } user)
            return new GenericResponse<ChatUser>(null, WebSocketConstants.InvalidSessionId);

        user.SessionId = null;
        await context.SaveChangesAsync();
        
        return new GenericResponse<ChatUser>(user, null);
    }
}