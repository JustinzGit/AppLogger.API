using AppLogger.API.Abstractions;
using AppLogger.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AppLogger.API.Features.Logging;

public static class GetAppNames
{
    public static async Task<IResult> Handler(AppLoggerCache appLoggerCache, CancellationToken requestToken)
    {
        string[] apps = await appLoggerCache.GetOrRefreshAsync(
            "appNames", 
            FetchAppNamesAsync, 
            softTTL: TimeSpan.FromHours(1), 
            hardTTL: null,
            requestToken
        );

        return Results.Ok(apps);
    }

    private static async Task<string[]> FetchAppNamesAsync(AppLoggerContext appLoggerContext, CancellationToken token)
    {
        appLoggerContext.Database.SetCommandTimeout(300); // 5 minutes

        await using var transaction = await appLoggerContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, token);

        string[] apps = await appLoggerContext.Logs
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