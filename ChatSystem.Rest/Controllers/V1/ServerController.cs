using System.Text.Json;
using AspNetCore.ClaimsValueProvider;
using ChatSystem.Authorization.Models;
using ChatSystem.Data;
using ChatSystem.Data.Dtos;
using ChatSystem.Data.Models;
using ChatSystem.Logic.Helpers;
using ChatSystem.Logic.Models.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ChatSystem.Rest.Controllers.V1;

//ToDo: All UPDATE, POST, PUT requests have to cache data so all GET requests can fetch from cached data only
//ToDo: add a bunch of bs GET requests for pretty much every single thing to fucking exist
//Todo: GET on members of large servers has to be paged and done lazily

[Authorize]
[ApiController]
[Route("api/v1/server")]
[EnableRateLimiting("Api")]
public class ServerController : ControllerBase
{
    private readonly IDbContextFactory<EntityFrameworkContext> _dbContext;

    public ServerController(IDbContextFactory<EntityFrameworkContext> dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateServer([FromClaim(ChatClaims.UserId)] Guid userId, CreateServerRequest request)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        var server = (ChatServer)request;
        server.Id = Guid.NewGuid();
        server.OwnerId = userId;
        server.Members = new List<ChatServerMember>
        {
            new ChatServerMember
            {   
                UserId = userId,
                ServerId = server.Id,
                Permissions = ChatPermissions.CanKick | ChatPermissions.CanBan | ChatPermissions.CanViewLogs |
                              ChatPermissions.RoleControl | ChatPermissions.CanCreateInvite | ChatPermissions.CanCreateChannels,
            }
        };
        
        context.Servers.Add(server);
        await context.SaveChangesAsync();

        return Ok(JsonSerializer.Serialize((BasicChatServer)server, JsonHelper.JsonSerializerOptions));
    }

    [HttpDelete("{serverId:guid}/delete")]
    public async Task<IActionResult> DeleteServer([FromClaim(ChatClaims.UserId)] Guid userId, Guid serverId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        if (await context.Servers.FirstOrDefaultAsync(x => x.Id == serverId && x.OwnerId == userId) is not { } server)
            return Unauthorized();

        context.Servers.Remove(server);
        await context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{serverId:guid}/view-logs")]
    public async Task<IActionResult> ViewLogs([FromClaim(ChatClaims.UserId)] Guid userId, Guid serverId, uint page = 1)
    {
        const int pageSize = 15;

        await using var context = await _dbContext.CreateDbContextAsync();

        var serverMember = await context.ServerMembers
            .AsNoTracking()
            .Include(sm => sm.Server)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ServerId == serverId);

        if (serverMember is null)
            return Ok();

        if (!serverMember.Permissions.HasFlag(ChatPermissions.CanViewLogs))
            return Unauthorized();

        List<BasicChatServerLog> logs = context.ServerLogs
            .Where(log => log.ServerId == serverId)
            .OrderByDescending(log => log.Timestamp)
            .Skip((int)((page - 1) * pageSize))
            .Take(pageSize)
            .Select(log => (BasicChatServerLog)log)
            .ToList();

        return Ok(logs);
    }

    [HttpPost("{serverId:guid}/create-invite")]
    public async Task<IActionResult> CreateInvite([FromClaim(ChatClaims.UserId)] Guid userId, Guid serverId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        var serverMember = await context.ServerMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ServerId == serverId);

        if (serverMember is null)
            return BadRequest();

        if (!serverMember.Permissions.HasFlag(ChatPermissions.CanCreateInvite))
            return Unauthorized();

        var invite = new ChatServerInvite()
        {
            InviteCode = RandomGenerator.String(16),
            ServerId = serverId
        };
        
        context.ServerInvites.Add(invite);

        await context.SaveChangesAsync();

        return Ok(invite.InviteCode);
    }

    [HttpDelete("{inviteId:guid}")]
    public async Task<IActionResult> DeleteInvite([FromClaim(ChatClaims.UserId)] Guid userId, Guid serverId, Guid inviteId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        var serverMember = await context.ServerMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ServerId == serverId);

        if (serverMember is null)
            return BadRequest();

        if (!serverMember.Permissions.HasFlag(ChatPermissions.CanKick))
            return Unauthorized();

        var invite = await context.ServerInvites.FirstOrDefaultAsync(x => x.Id == inviteId);
        if (invite is null)
            return BadRequest();

        context.Remove(invite);
        await context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{invite}/join")]
    public async Task<IActionResult> JoinServer([FromClaim(ChatClaims.UserId)] Guid userId, string invite)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        ChatServerInvite? serverInvite = await context.ServerInvites.FirstOrDefaultAsync(x => x.InviteCode == invite);
        if (serverInvite is null)
            return Ok(); // not found

        if (await context.ServerBannedUsers.FirstOrDefaultAsync(x => x.UserId == userId) is not null)
            return Unauthorized(); //banned

        if (await context.ServerMembers.FirstOrDefaultAsync(x =>
                x.UserId == userId && x.ServerId == serverInvite.ServerId) is not null)
            return BadRequest(); // already member
        
        var serverMember = new ChatServerMember
        {
            UserId = userId,
            ServerId = serverInvite.ServerId
        };
        
        context.ServerMembers.Add(serverMember);
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    [HttpPost("{serverId:guid}/leave")]
    public async Task<IActionResult> LeaveServer([FromClaim(ChatClaims.UserId)] Guid userId, Guid serverId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        var serverMember = await context.ServerMembers.FirstOrDefaultAsync(x => x.UserId == userId && x.ServerId == serverId);
        if (serverMember is null)
            return BadRequest();

        if (serverMember.Server.OwnerId == userId)
            return BadRequest(); //must call delete
            
        context.ServerMembers.Remove(serverMember);
        await context.SaveChangesAsync();
        
        return Ok();
    }

    [HttpPut("{serverId:guid}/create-role")]
    public async Task<IActionResult> CreateRole([FromClaim(ChatClaims.UserId)] Guid userId, Guid serverId, CreateRoleRequest request)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        var serverMember = await context.ServerMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ServerId == serverId);

        if (serverMember is null)
            return BadRequest();

        if (!serverMember.Permissions.HasFlag(ChatPermissions.RoleControl))
            return Unauthorized();

        context.ServerRoles.Add((ChatServerRole)request);

        await context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{serverId:guid}/delete-role")]
    public async Task<IActionResult> DeleteRole([FromClaim(ChatClaims.UserId)] Guid userId, Guid serverId,
        Guid targetRoleId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        var serverMember = await context.ServerMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ServerId == serverId);

        if (serverMember is null)
            return BadRequest();

        if (!serverMember.Permissions.HasFlag(ChatPermissions.RoleControl))
            return Unauthorized();

        ChatServerRole? serverRole = await context.ServerRoles.FirstOrDefaultAsync(x => x.Id == targetRoleId);
        if (serverRole is null)
            return BadRequest();

        context.Remove(serverRole);
        await context.SaveChangesAsync();

        return Ok();
    }

    private async Task<(ChatServerMember? moderator, ChatServerMember? targetUser)> GetModeratorAndTargetUserAsync(Guid userId, Guid serverId, Guid targetUserId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        var moderator = await context.ServerMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ServerId == serverId);

        var targetUser = await context.ServerMembers
            .FirstOrDefaultAsync(x => x.UserId == targetUserId && x.ServerId == serverId);

        return (moderator, targetUser);
    }

    [HttpPost("{serverId:guid}/kick-member")]
    public async Task<IActionResult> KickMember([FromClaim(ChatClaims.UserId)] Guid userId, Guid serverId, Guid targetUserId)
    {
        var (moderator, targetUser) = await GetModeratorAndTargetUserAsync(userId, serverId, targetUserId);
        if (moderator is null || targetUser is null)
            return BadRequest();
        
        if (!moderator.Permissions.HasFlag(ChatPermissions.CanKick))
            return Unauthorized();

        await using var context = await _dbContext.CreateDbContextAsync();
        context.ServerMembers.Remove(targetUser);
        await context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{serverId:guid}/ban-member")]
    public async Task<IActionResult> BanMember([FromClaim(ChatClaims.UserId)] Guid userId, Guid serverId, Guid targetUserId)
    {
        var (moderator, targetUser) = await GetModeratorAndTargetUserAsync(userId, serverId, targetUserId);
        if (moderator is null || targetUser is null)
            return BadRequest();
        
        if (!moderator.Permissions.HasFlag(ChatPermissions.CanBan))
            return Unauthorized();

        await using var context = await _dbContext.CreateDbContextAsync();
        context.ServerBannedUsers.Add(targetUser);
        context.ServerMembers.Remove(targetUser);
        await context.SaveChangesAsync();

        return Ok();
    }
}