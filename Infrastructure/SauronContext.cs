using Microsoft.EntityFrameworkCore;
using Sauron.API.Infrastructure.Entities;

namespace Sauron.API.Infrastructure;

public class SauronContext(DbContextOptions<SauronContext> options) : DbContext(options)
{
    public DbSet<Log> Logs { get; set; }
}