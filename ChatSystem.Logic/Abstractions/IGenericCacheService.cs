namespace ChatSystem.Logic.Abstractions;

public interface IGenericCacheService
{
    public void CacheValue<TFull, TBasic>(TFull full);
    public TBasic? FetchValue<TBasic>(string key);
}