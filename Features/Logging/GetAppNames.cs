using Sauron.API.Abstractions;
using Sauron.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Sauron.API.Features.Logging;

public static class GetAppNames
{
    public static async Task<IResult> Handler(SauronCache sauronCache)
    {
        List<string> apps = await sauronCache.GetOrRefreshAsync(
            "appNames", 
            FetchAppNamesAsync, 
            softExpiration: TimeSpan.FromHours(1), 
            hardExpiration: TimeSpan.FromHours(24)
        );

        return Results.Ok(apps);
    }

    private static async Task<List<string>> FetchAppNamesAsync(SauronContext sauronContext)
    {
        await using var transaction = await sauronContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);

        List<string> apps = await sauronContext.Logs
            .Select(l => l.App)
            .Distinct()
            .ToListAsync();

        await transaction.CommitAsync();

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