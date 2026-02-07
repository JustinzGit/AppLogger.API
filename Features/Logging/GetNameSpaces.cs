using Sauron.API.Abstractions;
using Sauron.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Sauron.API.Features.Logging;

public static class GetNameSpaces
{
    public static async Task<IResult> Handler(SauronCache sauronCache, CancellationToken requestToken)
    {
        string[] namespaces = await sauronCache.GetOrRefreshAsync(
            "namespaces", 
            FetchNameSpacesAsync, 
            softTTL: TimeSpan.FromHours(1), 
            hardTTL: null,
            requestToken
        );

        return Results.Ok(namespaces);
    }

    private static async Task<string[]> FetchNameSpacesAsync(SauronContext sauronContext, CancellationToken token)
    {
        sauronContext.Database.SetCommandTimeout(300); // 5 minutes
        
        using var transaction = await sauronContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, token);

        string[] namespaces = await sauronContext.Logs
            .Where(l => l.SourceContext != null)
            .Select(l => l.SourceContext!)
            .Distinct()
            .OrderByDescending(n => n.Length)
            .ToArrayAsync(token);

        await transaction.CommitAsync(token);

        return namespaces;
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/logging/name-spaces", Handler);
        }
    }
}