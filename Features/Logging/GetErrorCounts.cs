using Sauron.API.Abstractions;
using Sauron.API.Infrastructure;
using Sauron.API.Infrastructure.Entities;

namespace Sauron.API.Features.Logging;

public static class GetErrorCounts
{
    public static async Task<IResult> Handler(SauronContext sauronContext)
    {
        DateTime currentDate = DateTime.Today;

        IQueryable<Log> errorLogs = sauronContext.Logs.Where(l => 
            l.LogDay == currentDate.Day
            && l.LogTime >= currentDate
            && l.Level == "Error"
        );

        var errorCountsByApp = errorLogs
            .GroupBy(l => l.App)
            .Select(g => new { App = g.Key, Count = g.Count() })
            .ToList();
        
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