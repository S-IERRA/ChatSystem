using ChatSystem.Data.Caching;
using StackExchange.Redis;

namespace ChatSystem.Logic.Websocket;

public static class RedisUserCache
{
    private static readonly ConnectionMultiplexer CacheClient = RedisConnectionManager.Connection;
    private static readonly IDatabase CacheDb = CacheClient.GetDatabase();
    
    private const string CacheKey = "Sessions:";

    public static async Task CacheChannelUser(string channelId, string sessionId)
    {
        var hashKey = CacheKey + channelId;
        await CacheDb.HashSetAsync(hashKey, sessionId, "1");
    }

    public static async Task<List<string>> GetChannelUsers(Guid channelId)
    {
        var hashKey = CacheKey + channelId;
        var hashEntries = await CacheDb.HashGetAllAsync(hashKey);
        return hashEntries.Select(entry => entry.Name.ToString()).ToList();
    }
}