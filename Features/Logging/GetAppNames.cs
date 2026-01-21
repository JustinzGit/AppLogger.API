using Sauron.API.Abstractions;
using Sauron.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Sauron.API.Features.Logging;

public static class GetAppNames
{
    public static async Task<IResult> Handler(SauronContext sauronContext)
    {        
        using var transaction = await sauronContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);

        List<string> apps = await sauronContext.Logs
            .Select(l => l.App)
            .Distinct()
            .ToListAsync();

        await transaction.CommitAsync();
        
        return Results.Ok(apps);
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/logging/app-names", Handler);
        }
    }
}