using Sauron.API.Abstractions;
using Sauron.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Sauron.API.Features.Logging;

public static class GetAppNames
{
    public static async Task<IResult> Handler(SauronCache sauronCache, CancellationToken requestToken)
    {
        string[] apps = await sauronCache.GetOrRefreshAsync(
            "appNames", 
            FetchAppNamesAsync, 
            softTTL: TimeSpan.FromHours(1), 
            hardTTL: null,
            requestToken
        );

        return Results.Ok(apps);
    }

    private static async Task<string[]> FetchAppNamesAsync(SauronContext sauronContext, CancellationToken token)
    {
        sauronContext.Database.SetCommandTimeout(300); // 5 minutes

        await using var transaction = await sauronContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, token);

        string[] apps = await sauronContext.Logs
            .Select(l => l.App)
            .Distinct()
            .ToArrayAsync(token);

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