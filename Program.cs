using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using AppLogger.API.Extensions;
using AppLogger.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpoints();
builder.Services.AddControllers();

string connectionString = builder.Configuration.GetConnectionString("AppLogger") ?? throw new Exception("Connection string has not been set");
builder.Services.AddDbContext<AppLoggerContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSingleton<AppLoggerCache>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    // httprepl parses openapi 3.0.x
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
});

builder.Services.AddHybridCache(o =>
{
    o.MaximumPayloadBytes = 50 * 1024 * 1024; // 50MB per entry
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<DailyLogSeederService>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppLoggerContext>();
    context.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapEndpoints();

app.Run();
