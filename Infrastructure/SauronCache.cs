using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Hybrid;

namespace Sauron.API.Infrastructure;

public class SauronCache(HybridCache cache, IServiceScopeFactory scopeFactory, ILogger<SauronCache> logger)
{
    private record CacheEntry(string[] Data, DateTimeOffset SoftTTL);

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _refreshLocks = new();

    public async Task<string[]> GetOrRefreshAsync(string cacheKey, Func<SauronContext, CancellationToken, Task<string[]>> fetch, TimeSpan softTTL, TimeSpan? hardTTL = null, CancellationToken requestToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        CacheEntry cacheEntry = await cache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(requestToken, token);
                string[] data = await FetchAsync(fetch, linked.Token);
                return new CacheEntry(data, DateTimeOffset.UtcNow.Add(softTTL));
            },
            options: new HybridCacheEntryOptions { Expiration = hardTTL },
            cancellationToken: requestToken
        );

        if (now <= cacheEntry.SoftTTL)
            return cacheEntry.Data;

        SemaphoreSlim refreshLock = _refreshLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

        if (refreshLock.Wait(0, CancellationToken.None))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    logger.LogInformation("Cache refresh in progress for key: {Key}", cacheKey);

                    string[] data = await FetchAsync(fetch, CancellationToken.None);

                    await cache.SetAsync(
                        cacheKey,
                        new CacheEntry(data, DateTimeOffset.UtcNow.Add(softTTL)),
                        options: new HybridCacheEntryOptions { Expiration = hardTTL }
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Cache refresh failed for key: {Key}", cacheKey);
                }
                finally
                {
                    logger.LogInformation("Cache refresh complete for key: {Key}", cacheKey);
                    refreshLock.Release();
                }
            }, CancellationToken.None);
        }
        return cacheEntry.Data;
    }

    private async Task<string[]> FetchAsync(Func<SauronContext, CancellationToken, Task<string[]>> fetch, CancellationToken token)
    {
        await using AsyncServiceScope serviceScope = scopeFactory.CreateAsyncScope();

        SauronContext sauronContext = serviceScope.ServiceProvider.GetRequiredService<SauronContext>();

        return await fetch(sauronContext, token);
    }
}
