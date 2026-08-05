using Microsoft.EntityFrameworkCore;
using AppLogger.API.Infrastructure.Entities;

namespace AppLogger.API.Infrastructure;

public class AppLoggerContext(DbContextOptions<AppLoggerContext> options) : DbContext(options)
{
    public DbSet<Log> Logs { get; set; }
}