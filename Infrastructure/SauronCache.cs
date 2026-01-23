using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace Sauron.API.Infrastructure;

public class SauronCache(IMemoryCache cache, IServiceScopeFactory scopeFactory, ILogger<SauronCache> logger)
{
    private record CacheEntry<T>(T Data, DateTimeOffset SoftExpiration);

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<T> GetOrRefreshAsync<T>(string cacheKey, Func<SauronContext, Task<T>> fetch, TimeSpan softExpiration, TimeSpan hardExpiration)
    {
        long start = Stopwatch.GetTimestamp();
        
        if (cache.TryGetValue(cacheKey, out CacheEntry<T>? cachedData) && cachedData is not null)
        {
            if (DateTimeOffset.UtcNow > cachedData.SoftExpiration)
            {
                SemaphoreSlim refreshLock = _locks.GetOrAdd($"{cacheKey}:refresh", _ => new SemaphoreSlim(1, 1));

                if (refreshLock.Wait(0))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            T data = await FetchAndCacheAsync(cacheKey, fetch, softExpiration, hardExpiration);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Refresh failed: {Key}", cacheKey);
                        }
                        finally
                        {
                            refreshLock.Release();
                        }
                    });
                }
            }
            return cachedData.Data;
        }

        SemaphoreSlim coldLock = _locks.GetOrAdd($"{cacheKey}:cold", _ => new SemaphoreSlim(1, 1));

        await coldLock.WaitAsync();

        try
        {
            if (cache.TryGetValue(cacheKey, out cachedData) && cachedData is not null)
                return cachedData.Data;

            T data = await FetchAndCacheAsync(cacheKey, fetch, softExpiration, hardExpiration);

            return data;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Data Fetch Failed: {Key}", cacheKey);
            throw;
        }
        finally
        {
            coldLock.Release();
        }
    }

    private async Task<T> FetchAndCacheAsync<T>(string key, Func<SauronContext, Task<T>> fetch, TimeSpan softExpiration, TimeSpan hardExpiration)
    {
        using IServiceScope serviceScope = scopeFactory.CreateScope();

        SauronContext sauronContext = serviceScope.ServiceProvider.GetRequiredService<SauronContext>();

        T data = await fetch(sauronContext);

        CacheEntry<T> entry = new(data, DateTimeOffset.UtcNow.Add(softExpiration));

        cache.Set(key, entry, hardExpiration);

        return data;
    }
}
