using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sauron.API.Abstractions;
using Sauron.API.Infrastructure;
using Sauron.API.Infrastructure.Entities;

namespace SauronAPI.Features.Logging;

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
        bool SortDescending = true
    );

    public record Response(
        List<Log> Logs,
        int Limit,
        bool HasMore,
        int? CursorId
    );

    public static async Task<IResult> Handler([AsParameters] Request request, SauronContext sauronContext, ILoggerFactory loggerFactory, CancellationToken token)
    {
        ILogger logger = loggerFactory.CreateLogger("SearchLogs");

        IQueryable<Log> query = sauronContext.Logs.AsNoTracking();

        if (request.CursorId.HasValue)
        {
            query = request.SortDescending
                ? query.Where(l => l.Id < request.CursorId)
                : query.Where(l => l.Id > request.CursorId);
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

        try
        {
            sauronContext.Database.SetCommandTimeout(60); // 1 minute

            using var transaction = await sauronContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadUncommitted, token);

            List<Log> logs = await (
                    request.SortDescending
                        ? query.OrderByDescending(l => l.Id)
                        : query.OrderBy(l => l.Id)
                )
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
        catch (SqlException ex) when (ex.Number == -2)
        {
            logger.LogWarning("Status Code: {StatusCode}. Incoming Request: {@Request}.", 504, request);
            return Results.StatusCode(504);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Status Code: {StatusCode}. Incoming Request: {@Request}.", 499, request);
            return Results.StatusCode(499);
        }
        catch (Exception)
        {
            logger.LogError("Status Code: {StatusCode}. Incoming Request: {@Request}.", 500, request);
            return Results.StatusCode(500);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/logging/logs", Handler);
        }
    }
}