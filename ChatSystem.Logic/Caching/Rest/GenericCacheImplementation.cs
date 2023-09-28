using System.Text.Json;
using ChatSystem.ApiWrapper.Helpers;
using ChatSystem.Data.Caching;
using ChatSystem.Logic.Abstractions;
using Mapster;
using StackExchange.Redis;

namespace ChatSystem.Logic.Caching.Rest;

public class GenericCacheImplementation : IGenericCacheService
{
    //ToDo : use abstraction to set the key from the class name
    private static readonly ConnectionMultiplexer CacheClient = RedisConnectionManager.Connection;
    private static readonly IDatabase CacheDb = CacheClient.GetDatabase();

    public void CacheValue<TFull, TBasic>(TFull full)
    {
        if (full is null)
            return;
        
        TBasic basicType = full.Adapt<TBasic>();
        string cacheKey = typeof(TBasic).Name + ':' + GetId(full);

        string serialized = JsonSerializer.Serialize(basicType);
        
        CacheDb.HashSet(cacheKey, serialized, "1");
    }
    
    public TBasic? FetchValue<TBasic>(string key)
    {
        string cacheKey = typeof(TBasic).Name + ':' + key;
        string? serialized = CacheDb.StringGet(cacheKey);

        if (string.IsNullOrEmpty(serialized)) 
            return default;
        
        JsonHelper.TryDeserialize<TBasic>(serialized, out var basicType);
        
        return basicType;
    }
    
    private static string GetId<TFull>(TFull full)
    {
        var propertyInfo = typeof(TFull).GetProperty("Id");
        if (propertyInfo == null)
            throw new InvalidOperationException("Id property not found or is null/empty.");
        
        var idValue = propertyInfo.GetValue(full)?.ToString();
        if (!string.IsNullOrEmpty(idValue))
            return idValue;
        
        throw new InvalidOperationException("Id property not found or is null/empty.");
    }
}