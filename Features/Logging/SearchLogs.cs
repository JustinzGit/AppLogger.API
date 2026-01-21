using Microsoft.EntityFrameworkCore;
using Sauron.API.Abstractions;
using Sauron.API.Infrastructure;
using Sauron.API.Infrastructure.Entities;

namespace Sauron.API.Features.Logging;

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
        int Limit = 10
    );

    public record Response(
        List<Log> Logs,
        int Limit,
        bool HasMore,
        int? CursorId
    );

    public static async Task<IResult> Handler([AsParameters] Request request, SauronContext sauronContext)
    {
        IQueryable<Log> query = sauronContext.Logs.AsNoTracking();

        if (request.CursorId.HasValue)
            query = query.Where(l => l.Id < request.CursorId);

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

        List<Log> logs = await query
            .OrderByDescending(l => l.Id)
            .Take(request.Limit + 1)
            .ToListAsync();

        bool hasMore = logs.Count > request.Limit;
        if (hasMore) logs.RemoveAt(logs.Count - 1);
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