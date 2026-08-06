namespace AppLogger.API.Infrastructure;

public class DailyLogSeederService(IServiceScopeFactory scopeFactory, ILogger<DailyLogSeederService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppLoggerContext>();
                context.Database.EnsureCreated();
                DbSeeder.Seed(context);
                DbSeeder.PruneOldLogs(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Daily log seeding failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
