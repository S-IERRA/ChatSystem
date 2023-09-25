using System.Net;
using AspNetCore.ClaimsValueProvider;
using ChatSystem.Authorization.Models;
using ChatSystem.Data;
using ChatSystem.Data.Dtos;
using ChatSystem.Data.Models;
using ChatSystem.Logic.Abstractions;
using ChatSystem.Logic.Models.Rest;
using ChatSystem.Logic.Models.Websocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ChatSystem.Rest.Controllers.V1;

[Authorize]
[ApiController]
[Route("api/v1/messages")]
[EnableRateLimiting("Api")]
public class MessageController  : ControllerBase
{
    private readonly IDbContextFactory<EntityFrameworkContext> _dbContext;
    private readonly IRedisCommunicationService _redisCommunication;
    
    public MessageController(IDbContextFactory<EntityFrameworkContext> dbContext, IRedisCommunicationService redisCommunication)
    {
        _dbContext = dbContext;
        _redisCommunication = redisCommunication;
    }

    //There is no need to check whether the user is friends with the recipient because there is already a check on channel creation
    [HttpPost("{channelId:guid}/send")]
    public async Task<IActionResult> Send([FromClaim(ChatClaims.UserId)] Guid userId, Guid channelId, string content)
    {
        await using var context = await _dbContext.CreateDbContextAsync();

        if (await context.Channels.Where(x => x.Id == channelId)
                .Include(x=>x.Users)
                .FirstOrDefaultAsync() is not { } 
                channel)
            return NotFound();

        if (channel.Users.FirstOrDefault(x => x.Id == userId) is null)
            return BadRequest();

        var newMessage = new ChatMessage()
        {
            AuthorId = userId,
            ChannelId = channelId,
            Content = content
        };

        context.Messages.Add(newMessage);
        await context.SaveChangesAsync();

        await _redisCommunication.SendViaChannelAsync(RedisEventTypes.NewMessage, channelId, (BasicMessage)newMessage);
        
        return Ok();
    }
    
    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromClaim(ChatClaims.UserId)] Guid userId, Guid messageId)
    {
        await using var context = await _dbContext.CreateDbContextAsync();
        
        if (context.Messages.FirstOrDefault(x => x.Id == messageId && x.AuthorId == userId) is not { } message)
            return BadRequest();

        context.Messages.Remove(message);
        await context.SaveChangesAsync();
        
        await _redisCommunication.SendViaChannelAsync(RedisEventTypes.DeleteMessage, message.ChannelId, (BasicMessage)message);

        return Ok();
    }
    
    [HttpPatch("edit")]
    public async Task<IActionResult> Edit([FromClaim(ChatClaims.UserId)] Guid userId, Guid messageId, string content)
    {
        await using var context = await _dbContext.CreateDbContextAsync();
        
        if (context.Messages.FirstOrDefault(x => x.Id == messageId && x.AuthorId == userId) is not { } message)
            return BadRequest();

        message.Content = content;
        await context.SaveChangesAsync();
        
        await _redisCommunication.SendViaChannelAsync(RedisEventTypes.EditMessage, message.ChannelId, (BasicMessage)message);

        return Ok();
    }
}