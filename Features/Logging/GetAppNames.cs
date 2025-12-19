using Sauron.API.Abstractions;
using Sauron.API.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Sauron.API.Features.Logging;

public static class GetAppNames
{
    public static async Task<IResult> Handler(SauronContext sauronContext)
    {        
        List<string> apps = await sauronContext.Logs
            .Select(l => l.App)
            .Distinct()
            .ToListAsync();
        
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