using AppLogger.API.Abstractions;
using AppLogger.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AppLogger.API.Features.Logging;

public static class GetNameSpaces
{
    public static async Task<IResult> Handler(AppLoggerCache appLoggerCache, CancellationToken requestToken)
    {
        string[] namespaces = await appLoggerCache.GetOrRefreshAsync(
            "namespaces", 
            FetchNameSpacesAsync, 
            softTTL: TimeSpan.FromHours(1), 
            hardTTL: null,
            requestToken
        );

        return Results.Ok(namespaces);
    }

    private static async Task<string[]> FetchNameSpacesAsync(AppLoggerContext appLoggerContext, CancellationToken token)
    {
        appLoggerContext.Database.SetCommandTimeout(300); // 5 minutes
        
        using var transaction = await appLoggerContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, token);

        string[] namespaces = await appLoggerContext.Logs
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