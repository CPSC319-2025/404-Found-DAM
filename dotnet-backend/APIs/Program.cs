using Microsoft.EntityFrameworkCore;
using APIs.Controllers;
using Infrastructure.DataAccess;
using Core.Interfaces;
using Core.Services;
using MockedData;

var builder = WebApplication.CreateBuilder(args);


//Note to developers: need to add to appsettings.json -> "AllowedOrigins": [FRONTENDROUTEGOESHERE],
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Replace the existing DbContext registration with this:
builder.Services.AddDbContextFactory<DAMDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services with the dependency injection container
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectRepository, EFCoreProjectRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAdminRepository, EFCoreAdminRepository>();
builder.Services.AddScoped<IPaletteService, PaletteService>();
builder.Services.AddScoped<IPaletteRepository, PaletteRepository>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ISearchRepository, SearchRepository>();



var app = builder.Build();

app.UseCors("AllowReactApp");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Run "dotnet run --seed" to seed database
if (args.Contains("--seed"))
{
    await SeedDatabase(app);
}

async Task SeedDatabase(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
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
}

// Extension methods to register and group endpoints by controller
app.MapProjectEndpoints(); 
app.MapNotificationEndpoints(); 
app.MapAdminEndpoints(); 
app.MapPaletteEndpoints(); 
app.MapSearchEndpoints();

if (app.Environment.IsDevelopment())
{
    await using (var serviceScope = app.Services.CreateAsyncScope())
    await using (var context = serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<DAMDbContext>>().CreateDbContext())
    {
        await context.Database.EnsureCreatedAsync();
    }
} else {
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DAMDbContext>();
            dbContext.Database.Migrate();
            Console.WriteLine("Database migrations applied successfully");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while applying migrations: {ex.Message}");
    }
}

app.Run();

public partial class Program { }
