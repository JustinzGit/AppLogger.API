using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Sauron.API.Extensions;
using Sauron.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpoints();
builder.Services.AddControllers();

string connectionString = builder.Configuration.GetConnectionString("Sauron") ?? throw new Exception("Connection string has not been set");
builder.Services.AddDbContext<SauronContext>(options => options.UseSqlServer(connectionString));

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Seed the database
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SauronContext>();
    context.Database.EnsureCreated();
    DbSeeder.Seed(context);
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapEndpoints();

app.Run();
