using System.Data;
using Microsoft.EntityFrameworkCore;
using AppLogger.API.Abstractions;
using AppLogger.API.Infrastructure;
using AppLogger.API.Infrastructure.Entities;

namespace AppLogger.API.Features.Logging;

public static class SearchLogs
{
    public record Request(
        DateTime? StartDateTime,
        DateTime? EndDateTime,
        byte? LogDay,
        string[]? Apps,
        string[]? Levels,
        string[]? IncludedNamespaces,
        string[]? ExcludedNamespaces,
        int? CursorId,
        int Limit = 10,
        bool SortAscending = true
    );

    public record Response(
        List<Log> Logs,
        int Limit,
        bool HasMore,
        int? CursorId
    );

    public static async Task<IResult> Handler([AsParameters] Request request, AppLoggerContext appLoggerContext, ILoggerFactory loggerFactory, CancellationToken token)
    {
        ILogger logger = loggerFactory.CreateLogger("SearchLogs");

        IQueryable<Log> query = appLoggerContext.Logs.AsNoTracking();

        if (request.CursorId.HasValue)
        {
            query = request.SortAscending
                ? query.Where(l => l.Id > request.CursorId)
                : query.Where(l => l.Id < request.CursorId);
        }

        if (request.StartDateTime.HasValue)
            query = query.Where(l => l.LogTime >= request.StartDateTime);

        if (request.EndDateTime.HasValue)
            query = query.Where(l => l.LogTime <= request.EndDateTime);

        if (request.LogDay.HasValue)
            query = query.Where(l => l.LogDay == request.LogDay);

        if (request.Apps?.Length > 0)
            query = query.Where(l => request.Apps.Contains(l.App));

        if (request.Levels?.Length > 0)
            query = query.Where(l => request.Levels.Contains(l.Level));

        if (request.IncludedNamespaces?.Length > 0)
            query = query.Where(l => request.IncludedNamespaces.Contains(l.SourceContext));

        if (request.ExcludedNamespaces?.Length > 0)
            query = query.Where(l => !request.ExcludedNamespaces.Contains(l.SourceContext));

        if (request.SortAscending)
            query = query.OrderBy(l => l.Id);
        else
            query = query.OrderByDescending(l => l.Id);

        appLoggerContext.Database.SetCommandTimeout(60); // 1 minute

        using var transaction = await appLoggerContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, token);

        List<Log> logs = await query
            .Take(request.Limit + 1)
            .ToListAsync(token);

        await transaction.CommitAsync(token);

        bool hasMore = logs.Count > request.Limit;

        if (hasMore)
            logs.RemoveAt(logs.Count - 1);

        int? cursorId = logs.Count != 0 ? logs.Last().Id : null;

        Response response = new(logs, request.Limit, hasMore, cursorId);

        return Results.Ok(response);
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/logging/logs", Handler);
        }
    }
}