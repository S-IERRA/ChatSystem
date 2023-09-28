using System.Text.Json;
using AspNetCore.ClaimsValueProvider;
using ChatSystem.Authorization.Models;
using ChatSystem.Data;
using ChatSystem.Data.Dtos;
using ChatSystem.Data.Models;
using ChatSystem.Logic.Abstractions;
using ChatSystem.Logic.Helpers;
using ChatSystem.Logic.Models.Rest;
using ChatSystem.Logic.Models.Websocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ChatSystem.Rest.Controllers.V1;

[Authorize]
[ApiController]
[Route("api/v1/channels")]
[EnableRateLimiting("Api")]
public class ChannelsController : ControllerBase
{
    private readonly IDbContextFactory<EntityFrameworkContext> _dbContext;
    private readonly IRedisCommunicationService _redisCommunication;

    public ChannelsController(IDbContextFactory<EntityFrameworkContext> dbContext, IRedisCommunicationService redisCommunication)
    {
        _dbContext = dbContext;
        _redisCommunication = redisCommunication;
    }

    // dm / group channels
    [HttpPost("create-group")]
    public async Task<IActionResult> CreateChannel([FromClaim(ChatClaims.UserId)] Guid creatorId, List<Guid> userIds)
    {
        if (userIds.Any(x => x == creatorId))
            return BadRequest();
        
        await using var context = await _dbContext.CreateDbContextAsync();
        var creatorUser = await context.Users
            .Where(x => x.Id == creatorId)
            .Include(x => x.Relationships)
            .ThenInclude(x=>x.Users)
            .FirstOrDefaultAsync();

        if (creatorUser is null)
            return NotFound();  

        var friendUsers = creatorUser.Relationships
            .Where(r => r.Type == ChatRelationShipType.Friends)
            .SelectMany(r => r.Users)
            .ToList();
        
        var users = friendUsers.Where(u => userIds.Contains(u.Id)).ToList();

        if (users.Count != userIds.Count)
            return BadRequest(); // must be friends with all users being added

        users.Add(creatorUser);
        
        string returnValue = "";
        ChatChannel chatChannel;
        switch (users.Count)
        {
            case 2:
            {
                chatChannel = new ChatChannel()
                {
                    Id = Guid.NewGuid(),
                    Type = ChatChannelType.Dm,
                    Name = users[0].Username, 
                    Users = users,
                };

                returnValue = JsonSerializer.Serialize((BasicDmChannel)chatChannel, JsonHelper.JsonSerializerOptions);
                break;
            }

            case > 2 and <= 10:
            {
                chatChannel = new ChatChannel()
                {
                    Id = Guid.NewGuid(),
                    ModeratorId = creatorId,
                    Type = ChatChannelType.Group,
                    Name = String.Join(", ", users.Select(x => x.Username)),
                    Users = users,
                };

                returnValue = JsonSerializer.Serialize((BasicGroupChannel)chatChannel, JsonHelper.JsonSerializerOptions);
                break;
            }

            default:
                return BadRequest();
        }
        
        foreach (var user in users.Where(user => user.SessionId is not null))
            await _redisCommunication.SendViaSessionAsync(RedisEventTypes.CreateNewChannel, user.SessionId.Value, returnValue);
        
        context.Channels.Add(chatChannel);
        await context.SaveChangesAsync();
        
        return Ok(returnValue);
    }

    // server
    [HttpPost("create-server")]
    public async Task<IActionResult> CreateChannel([FromClaim(ChatClaims.UserId)] Guid creatorId, Guid serverId, CreateServerChannelRequest request)
    {
        await using var context = await _dbContext.CreateDbContextAsync();
        
        var serverMember = await context.ServerMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == creatorId && x.ServerId == serverId);

        if (serverMember is null)
            return BadRequest();

        if (!serverMember.Permissions.HasFlag(ChatPermissions.CanCreateChannels))
            return Unauthorized();

        var chatChannel = (ChatChannel)request;
        chatChannel.ServerId = serverId;
        
        context.Channels.Add(chatChannel);
        await context.SaveChangesAsync();
        
        return Ok();
    }

    [HttpDelete("delete-channel")]
    public async Task<IActionResult> DeleteChannel([FromClaim(ChatClaims.UserId)] Guid userId, Guid channelId)
    {
        //ToDo determine channel type, if server we have to check permissions if gc or regular then check if moderator if dm bad request
        
        await using var context = await _dbContext.CreateDbContextAsync();

        if (await context.Channels.FirstOrDefaultAsync(x => x.Id == channelId) is not { } channel)
            return BadRequest();

        if (channel.ModeratorId is not null && userId != channel.ModeratorId)
            return BadRequest();
        /*else
            return BadRequest(); // dm channel
        //ToDo add permissions check for servers*/
            
        context.Channels.Remove(channel);
        await context.SaveChangesAsync();

        return Ok();
    }
}