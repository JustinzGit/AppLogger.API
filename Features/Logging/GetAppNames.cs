using Sauron.API.Abstractions;
using Sauron.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Extensions.Caching.Memory;

namespace Sauron.API.Features.Logging;

public static class GetAppNames
{
    public static async Task<IResult> Handler(SauronContext sauronContext, IMemoryCache cache)
    {
        const string cacheKey = "appNames";

        if (cache.TryGetValue(cacheKey, out List<string>? cachedAppNames))
        {
            return Results.Ok(cachedAppNames);
        }

        using var transaction = await sauronContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);

        List<string> apps = await sauronContext.Logs
            .Select(l => l.App)
            .Distinct()
            .ToListAsync();

        await transaction.CommitAsync();

        cache.Set(cacheKey, apps, TimeSpan.FromHours(24));

        return Results.Ok(apps);
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/logging/app-names", Handler);
        }
    }
}