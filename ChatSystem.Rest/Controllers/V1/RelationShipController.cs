using AspNetCore.ClaimsValueProvider;
using ChatSystem.Authorization.Models;
using ChatSystem.Data;
using ChatSystem.Data.Dtos;
using ChatSystem.Data.Models;
using ChatSystem.Logic.Abstractions;
using ChatSystem.Logic.ChatSystem_Logic;
using ChatSystem.Logic.Models.Rest;
using ChatSystem.Logic.Models.Websocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ChatSystem.Rest.Controllers.V1;

[Authorize]
[ApiController]
[Route("api/v1/relationships")]
[EnableRateLimiting("Api")] 
public class RelationShipController : ControllerBase
{
    private readonly IRedisCommunicationService _redisCommunication;
    private readonly IDbContextFactory<EntityFrameworkContext> _dbContext;

    public RelationShipController(IDbContextFactory<EntityFrameworkContext> dbContext, IRedisCommunicationService redisCommunication)
    {
        _dbContext = dbContext;
        _redisCommunication = redisCommunication;
    }

    [HttpGet("")]
    public async Task<IActionResult> FetchRelationships([FromClaim(ChatClaims.UserId)] Guid userId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        if (await context.Users.Where(x => x.Id == userId).Include(x=>x.Relationships).ThenInclude(x=>x.Users).FirstOrDefaultAsync() is not { } user)
            return NotFound();

        return Ok(user);
    }
    
    [AllowAnonymous]
    [HttpPost("test-method1")]
    public async Task<IActionResult> TestMethod1()
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        var user1Object = new CreateAccountRequest("Account1", "email1@gmail.com", "string");
        ChatUser chatUser1 = await context.CreateUserAccount(user1Object);
        chatUser1.RegistrationToken = Guid.Empty;
        
        var user2Object = new CreateAccountRequest("Account2", "email2@gmail.com", "string");
        ChatUser chatUser2 = await context.CreateUserAccount(user2Object);
        chatUser2.RegistrationToken = Guid.Empty;
        
        await context.SaveChangesAsync();

        await SendFriendRequest(chatUser1.Id, "Account2");
        await FriendRequest(chatUser1.Id, chatUser2.Id, true);

        return Ok();
    }

    //ToDo: is user already has a relationship of any sort , decline the request
    [HttpPost("{targetUsername}/send-friend-request")]
    public async Task<IActionResult> SendFriendRequest([FromClaim(ChatClaims.UserId)] Guid userId, string targetUsername)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        if (await context.Users.FirstOrDefaultAsync(x => x.Id == userId) is not { } user)
            return NotFound();

        if (await context.Users.Where(x => x.Username == targetUsername)
                .Include(x=>x.Relationships)
                .ThenInclude(x=>x.Users)
                .FirstOrDefaultAsync() is not { } targetUser)
            return NotFound();

        var hasRelationShip = targetUser.Relationships.FirstOrDefault(r => r.Users.Any(x=>x.Id == userId));
        if (hasRelationShip is not null)
            return BadRequest();

        var newRelationShip = new ChatRelationship()
        {
            CreatorId = userId,
            Type = ChatRelationShipType.Outgoing,
            Users = new List<ChatUser>()
            {
                user, targetUser
            }
        };
        
        context.Relationships.Add(newRelationShip);
        await context.SaveChangesAsync();

        if(user.SessionId is not null) 
            await _redisCommunication.SendViaSessionAsync(RedisEventTypes.OutgoingFriendRequest, user.SessionId.Value, (BasicChatRelationship)newRelationShip);

        return Ok();
    }

    [HttpDelete("{requestId:guid}/cancel-request")]
    public async Task<IActionResult> CancelFriendRequest([FromClaim(ChatClaims.UserId)] Guid userId, Guid requestId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        if (await context.Users.FirstOrDefaultAsync(x => x.Id == userId) is not { } user)
            return NotFound();
        
        if (await context.Relationships.FirstOrDefaultAsync(x => x.Id == requestId) is not { } relationship)
            return NotFound();

        if (relationship.CreatorId != userId)
            return BadRequest();

        context.Relationships.Remove(relationship);
        await context.SaveChangesAsync();
        
        if(user.SessionId is not null) 
            await _redisCommunication.SendViaSessionAsync(RedisEventTypes.CancelFriendRequest, user.SessionId.Value, (BasicChatRelationship)relationship);
        
        return Ok();
    }
    
    [HttpPost("{requestId:guid}/reply-friend-request")]
    public async Task<IActionResult> FriendRequest([FromClaim(ChatClaims.UserId)] Guid userId, Guid requestId, bool isAccepted)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        if (await context.Users.FirstOrDefaultAsync(x => x.Id == userId) is not { } user)
            return NotFound();

        if (await context.Relationships.FirstOrDefaultAsync(x => x.Id == requestId) is not { } relationship)
            return NotFound();

        if (relationship.CreatorId == userId)
            return BadRequest();
        
        if (isAccepted)
            relationship.Type = ChatRelationShipType.Friends;
        else
            context.Relationships.Remove(relationship);
        
        await context.SaveChangesAsync();
        
        if(user.SessionId is not null) 
            await _redisCommunication.SendViaSessionAsync(isAccepted ? RedisEventTypes.Friended : RedisEventTypes.DeclinedFriendRequest, user.SessionId.Value, (BasicChatRelationship)relationship);
        
        return Ok();
    }

    [HttpDelete("{targetUserId:guid}/unfriend")]
    public async Task<IActionResult> Unfriend([FromClaim(ChatClaims.UserId)] Guid userId, Guid targetUserId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        var currentUser = await context.Users
            .Include(u => u.Relationships)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (currentUser is null)
            return NotFound("User not found");

        var targetUser = await context.Users
            .Include(u => u.Relationships)
            .FirstOrDefaultAsync(u => u.Id == targetUserId);

        if (targetUser is null)
            return NotFound("Target user not found");

        var friendship = currentUser.Relationships.FirstOrDefault(r =>
            r.Type is ChatRelationShipType.Friends && r.Users.Any(x=>x.Id == targetUserId));

        if (friendship is null)
            return BadRequest("You are not friends with the target user");

        context.Relationships.Remove(friendship);
        await context.SaveChangesAsync();
        
        if(currentUser.SessionId is not null) 
            await _redisCommunication.SendViaSessionAsync(RedisEventTypes.Unfriended, currentUser.SessionId.Value, (BasicChatRelationship)friendship);
        
        return Ok();
    }
    
    //ToDo: if user is friends, remove that friendship
    [HttpPost("{targetUserId:guid}/block")]
    public async Task<IActionResult> BlockUser([FromClaim(ChatClaims.UserId)] Guid userId, Guid targetUserId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        if (await context.Users.FirstOrDefaultAsync(x => x.Id == userId) is not { } currentUser)
            return NotFound();

        if (await context.Users.FirstOrDefaultAsync(x => x.Id == targetUserId) is not { } targetUser)
            return NotFound();
        
        var friendship = currentUser.Relationships.FirstOrDefault(r => r.Type is ChatRelationShipType.Friends && r.Users.Any(x=>x.Id == targetUserId));
        if (friendship is not null)
            context.Relationships.Remove(friendship);

        var newBlockedRelationship = new ChatRelationship()
        {
            CreatorId = userId,
            Type = ChatRelationShipType.Blocked,
            Users = new List<ChatUser>()
            {
                currentUser, targetUser
            }
        };
        
        context.Relationships.Add(newBlockedRelationship);
        await context.SaveChangesAsync();
        
        if(currentUser.SessionId is not null) 
            await _redisCommunication.SendViaSessionAsync(RedisEventTypes.Blocked, currentUser.SessionId.Value, (BasicChatRelationship)newBlockedRelationship);

        return Ok();
    }
    
    [HttpPost("{targetUserId:guid}/unblock")]
    public async Task<IActionResult> UnblockUser([FromClaim(ChatClaims.UserId)] Guid userId, Guid targetUserId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        if (await context.Users.Where(x => x.Id == userId)
                .Include(x => x.Relationships)
                .ThenInclude(x=>x.Users)
                .FirstOrDefaultAsync() is not { } 
                currentUser)
            return NotFound();

        var friendship = currentUser.Relationships.FirstOrDefault(r => r.Type is ChatRelationShipType.Blocked && r.Users.Any(x=>x.Id == targetUserId));
        if (friendship is null)
            return BadRequest("You are not blocking the target user");

        context.Relationships.Remove(friendship);
        await context.SaveChangesAsync();
        
        if(currentUser.SessionId is not null) 
            await _redisCommunication.SendViaSessionAsync(RedisEventTypes.Unblocked, currentUser.SessionId.Value, (BasicChatRelationship)friendship);
        
        return Ok();
    }
}