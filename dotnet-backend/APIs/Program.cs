using Microsoft.EntityFrameworkCore;
using Infrastructure.DataAccess;
using APIs.Controllers;
using Core.Interfaces;
// using Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// dotnet ef migrations add InitialCreate --startup-project ../APIs
builder.Services.AddDbContext<DAMDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// builder.Services.AddScoped<ITestService, TestService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Extension methods to register and group endpoints by controller
app.MapPaletteEndpoints(); 

app.Run();
