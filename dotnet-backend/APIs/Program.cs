using Microsoft.EntityFrameworkCore;
using APIs.Controllers;
using Infrastructure.DataAccess;
using Core.Interfaces;
using Core.Services;
using MockedData;

var builder = WebApplication.CreateBuilder(args);

// 1) Read from appsettings.json -> "AllowedOrigins": ["http://localhost:3000"]
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("Content-Disposition");
        });
});

// 2) Add other services, swagger, etc.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContextFactory<DAMDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectRepository, EFCoreProjectRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAdminRepository, EFCoreAdminRepository>();
builder.Services.AddScoped<IPaletteService, PaletteService>();
builder.Services.AddScoped<IPaletteRepository, PaletteRepository>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ISearchRepository, SearchRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IImageService, ImageService>();

var app = builder.Build();

// 3) Make sure to call app.UseCors(...) BEFORE mapping endpoints
app.UseCors("AllowReactApp");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Optional: seed the database if the app is run with `--seed`
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

// Map your endpoints
app.MapProjectEndpoints();
app.MapNotificationEndpoints();
app.MapAdminEndpoints();
app.MapPaletteEndpoints();
app.MapSearchEndpoints();
app.MapUserEndpoints();

// Create/migrate database
if (app.Environment.IsDevelopment())
{
    await using var serviceScope = app.Services.CreateAsyncScope();
    await using var context = serviceScope.ServiceProvider
        .GetRequiredService<IDbContextFactory<DAMDbContext>>()
        .CreateDbContext();

    await context.Database.EnsureCreatedAsync();
}
else
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DAMDbContext>();
        dbContext.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while applying migrations: {ex.Message}");
    }
}

app.Run();

public partial class Program { }
