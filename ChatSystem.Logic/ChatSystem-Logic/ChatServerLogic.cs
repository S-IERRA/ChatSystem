using ChatSystem.Data;
using ChatSystem.Data.Models;
using ChatSystem.Logic.Models.Rest;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace ChatSystem.Logic.ChatSystem_Logic;

public class ChatServerService
{
    private readonly IDbContextFactory<EntityFrameworkContext> _dbContext;

    public ChatServerService(IDbContextFactory<EntityFrameworkContext> dbContext)
    {
        _dbContext = dbContext;
    }
}