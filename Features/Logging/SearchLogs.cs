using Sauron.API.Abstractions;
using Sauron.API.Infrastructure;
using Sauron.API.Infrastructure.Entities;
using Sauron.API.Models;

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
        int PageNumber = 1,
        int PageSize = 25,
        bool IncludeCount = true,
        bool OrderDescending = false
    );

    public static async Task<IResult> Handler([AsParameters] Request request, SauronContext sauronContext)
    {
        IQueryable<Log> query = sauronContext.Logs;

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

        query = request.OrderDescending ? query.OrderByDescending(l => l.Id) : query.OrderBy(l => l.Id);

        PagedList<Log> results = await PagedList<Log>.CreateAsync(query, request.PageNumber, request.PageSize, request.IncludeCount);

        return Results.Ok(results);
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/logging/logs", Handler);
        }
    }
}