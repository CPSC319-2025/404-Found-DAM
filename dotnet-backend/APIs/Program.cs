using Microsoft.EntityFrameworkCore;
using APIs.Controllers;
using Infrastructure.DataAccess;
using Core.Interfaces;
using Core.Services;
using MockedData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Server.Kestrel.Core;


var builder = WebApplication.CreateBuilder(args);

//// To increase request body size limit to 1 GB; Unblock and adjust if needed 
// builder.Services.Configure<KestrelServerOptions>(options =>
//    {
//      options.Limits.MaxRequestBodySize = 1_000_000_000;
//    });

//Note to developers: need to add to appsettings.json -> "AllowedOrigins": [FRONTENDROUTEGOESHERE],
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyOrigin()
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
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddTransient<IFileService, Core.Services.FileService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<ITagRepository, TagRepository>();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    string jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.");
    var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

// remove ! for azure testing
// Pay attention do not contact blob unless you are the 
// only developer working on this task. 
// Otherwise debugging will be a nightmare
// post on the backend channel if you are going to use this
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
} else {
    builder.Services.AddScoped<IBlobStorageService, LocalBlobStorageService>();
}

var app = builder.Build();

app.UseCors("AllowReactApp");

// Add Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<APIs.Middleware.AuthMiddleware>();


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
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapFileUploadEndpoints();
app.MapTagEndpoints();

// Create/migrate database
if (app.Environment.IsDevelopment())
{
    // Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI();
    await using var serviceScope = app.Services.CreateAsyncScope();
    await using var context = serviceScope.ServiceProvider
        .GetRequiredService<IDbContextFactory<DAMDbContext>>()
        .CreateDbContext();

    await context.Database.EnsureCreatedAsync();
} else if (Environment.GetEnvironmentVariable("RESET_DATABASE") == "true")
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DAMDbContext>();
    
    // Drop the database
    dbContext.Database.EnsureDeleted();
    
    // Apply migrations to create a new database
    dbContext.Database.Migrate();
    await SeedDatabase(app);
    
    Console.WriteLine("Database was reset and migrations applied successfully");
} else
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