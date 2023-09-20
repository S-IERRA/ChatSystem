using StackExchange.Redis;

namespace ChatSystem.Data.Caching;

//ToDO: Inject the value
public static class RedisConnectionManager
{
    private static readonly Lazy<ConnectionMultiplexer> LazyConnection = new(
        () => ConnectionMultiplexer.Connect("127.0.0.1:6379"));

    public static ConnectionMultiplexer Connection => LazyConnection.Value;
}