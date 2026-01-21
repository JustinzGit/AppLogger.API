using Sauron.API.Abstractions;
using Sauron.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Extensions.Caching.Memory;

namespace Sauron.API.Features.Logging;

public static class GetNameSpaces
{
    public static async Task<IResult> Handler(SauronContext sauronContext, IMemoryCache cache)
    {
        const string cacheKey = "namespaces";

        if (cache.TryGetValue(cacheKey, out List<string>? cachedNamespaces))
        {
            return Results.Ok(cachedNamespaces);
        }

        using var transaction = await sauronContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);

        List<string> namespaces = await sauronContext.Logs
            .Select(l => l.SourceContext)
            .OfType<string>()
            .Distinct()
            .OrderByDescending(n => n.Length)
            .ToListAsync();

        await transaction.CommitAsync();

        cache.Set(cacheKey, namespaces, TimeSpan.FromHours(24));

        return Results.Ok(namespaces);
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/logging/name-spaces", Handler);
        }
    }
}