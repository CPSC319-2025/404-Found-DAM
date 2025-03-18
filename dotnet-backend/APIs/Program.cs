using Microsoft.EntityFrameworkCore;
using APIs.Controllers;
using Infrastructure.DataAccess;
using Core.Interfaces;
using Core.Services;
using MockedData;

var builder = WebApplication.CreateBuilder(args);

// 1) Add CORS services and define a named policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocal3000", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000") // The React/Next.js URL
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // If you need cookies or auth from the browser
    });
});

// ... existing code ...

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Replace the existing DbContext registration with this:
builder.Services.AddDbContextFactory<DAMDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services with DI
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectRepository, EFCoreProjectRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAdminRepository, EFCoreAdminRepository>();
builder.Services.AddScoped<IPaletteService, PaletteService>();
builder.Services.AddScoped<IPaletteRepository, PaletteRepository>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ISearchRepository, SearchRepository>();

var app = builder.Build();

// 2) Use the CORS policy in the request pipeline
app.UseCors("AllowLocal3000");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Seed logic (unchanged)
if (args.Contains("--seed"))
{
    await SeedDatabase(app);
}

async Task SeedDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    try
    {
        Console.WriteLine("Start populating database with mocked data...");
        await MockedDataSeeding.Seed(scope);
        Console.WriteLine("Database seeding completed.");
    }
    catch (Exception)
    {
        throw;
    }
}

// Extension methods to register and group endpoints by controller
app.MapProjectEndpoints();
app.MapNotificationEndpoints();
app.MapAdminEndpoints();
app.MapPaletteEndpoints();
app.MapSearchEndpoints();

if (app.Environment.IsDevelopment())
{
    await using var serviceScope = app.Services.CreateAsyncScope();
    await using var context = serviceScope.ServiceProvider
        .GetRequiredService<IDbContextFactory<DAMDbContext>>()
        .CreateDbContext();
    await context.Database.EnsureCreatedAsync();
}

app.Run();

public partial class Program { }
