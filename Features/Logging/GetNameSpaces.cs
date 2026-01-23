using Sauron.API.Abstractions;
using Sauron.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Sauron.API.Features.Logging;

public static class GetNameSpaces
{
    public static async Task<IResult> Handler(SauronCache sauronCache)
    {
        List<string> namespaces = await sauronCache.GetOrRefreshAsync(
            "namespaces", 
            FetchNameSpacesAsync, 
            softExpiration: TimeSpan.FromHours(1), 
            hardExpiration: TimeSpan.FromHours(24)
        );

        return Results.Ok(namespaces);
    }

    private static async Task<List<string>> FetchNameSpacesAsync(SauronContext sauronContext)
    {
        using var transaction = await sauronContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);

        List<string> namespaces = await sauronContext.Logs
            .Select(l => l.SourceContext)
            .OfType<string>()
            .Distinct()
            .OrderByDescending(n => n.Length)
            .ToListAsync();

        await transaction.CommitAsync();

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