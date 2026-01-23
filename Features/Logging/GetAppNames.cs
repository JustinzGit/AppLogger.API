using Sauron.API.Abstractions;
using Sauron.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Sauron.API.Features.Logging;

public static class GetAppNames
{
    public static async Task<IResult> Handler(SauronCache sauronCache, CancellationToken requestToken)
    {
        List<string> apps = await sauronCache.GetOrRefreshAsync(
            "appNames", 
            FetchAppNamesAsync, 
            softTTL: TimeSpan.FromHours(1), 
            hardTTL: TimeSpan.FromHours(24),
            requestToken
        );

        return Results.Ok(apps);
    }

    private static async Task<List<string>> FetchAppNamesAsync(SauronContext sauronContext, CancellationToken token)
    {
        await using var transaction = await sauronContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, token);

        List<string> apps = await sauronContext.Logs
            .Select(l => l.App)
            .Distinct()
            .ToListAsync(token);

        await transaction.CommitAsync(token);

        return apps;
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/logging/app-names", Handler);
        }
    }
}