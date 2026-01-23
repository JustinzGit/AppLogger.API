using Sauron.API.Abstractions;
using Sauron.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Sauron.API.Features.Logging;

public static class GetNameSpaces
{
    public static async Task<IResult> Handler(SauronCache sauronCache, CancellationToken requestToken)
    {
        List<string> namespaces = await sauronCache.GetOrRefreshAsync(
            "namespaces", 
            FetchNameSpacesAsync, 
            softTTL: TimeSpan.FromHours(1), 
            hardTTL: TimeSpan.FromHours(24),
            requestToken
        );

        return Results.Ok(namespaces);
    }

    private static async Task<List<string>> FetchNameSpacesAsync(SauronContext sauronContext, CancellationToken token)
    {
        using var transaction = await sauronContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, token);

        List<string> namespaces = await sauronContext.Logs
            .Select(l => l.SourceContext)
            .OfType<string>()
            .Distinct()
            .OrderByDescending(n => n.Length)
            .ToListAsync(token);

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