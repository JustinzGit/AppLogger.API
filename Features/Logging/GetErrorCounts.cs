using System.Data;
using Microsoft.EntityFrameworkCore;
using AppLogger.API.Abstractions;
using AppLogger.API.Infrastructure;
using AppLogger.API.Infrastructure.Entities;

namespace AppLogger.API.Features.Logging;

public static class GetErrorCounts
{
    public static async Task<IResult> Handler(AppLoggerContext appLoggerContext, CancellationToken token)
    {
        DateTime currentDate = DateTime.Today;

        IQueryable<Log> errorLogs = appLoggerContext.Logs.Where(l => 
            l.LogDay == currentDate.Day
            && l.LogTime >= currentDate
            && l.Level == "Error"
        );

        appLoggerContext.Database.SetCommandTimeout(300); // 5 minutes
        
        using var transaction = await appLoggerContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, token);

        var errorCountsByApp = await errorLogs
            .GroupBy(l => l.App)
            .Select(g => new { App = g.Key, Count = g.Count() })
            .ToListAsync(token);

        await transaction.CommitAsync(token);
        
        return Results.Ok(errorCountsByApp);
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/logging/error-counts", Handler);
        }
    }
}