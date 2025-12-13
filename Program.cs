using Microsoft.EntityFrameworkCore;
using Sauron.API.Extensions;
using Sauron.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpoints();
builder.Services.AddControllers();
builder.Services.AddDbContext<SauronContext>(options => options.UseSqlite("Data Source=sauron.db"));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Seed the database
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<SauronContext>();
        context.Database.EnsureCreated();
        DbSeeder.Seed(context);
    }
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapEndpoints();

app.Run();
