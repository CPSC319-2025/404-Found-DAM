using Microsoft.EntityFrameworkCore;
using Infrastructure.DataAccess;

using APIs.Controllers;
using Core.Interfaces;
using Core.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// dotnet ef migrations add InitialCreate --startup-project ../APIs
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services with the dependency injection container
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IProjectRepository, EFCoreProjectRepository>();
builder.Services.AddScoped<IAdminRepository, EFCoreAdminRepository>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Extension methods to register and group endpoints by controller
app.MapProjectEndpoints(); 
app.MapNotificationEndpoints(); 
app.MapAdminEndpoints(); 

app.Run();


