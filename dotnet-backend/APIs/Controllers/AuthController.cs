using Core.Dtos;
using Core.Interfaces;

namespace APIs.Controllers
{
    public static class AuthController
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapPost("/auth/login", Login)
               .WithName("Login")
               .WithOpenApi();

            app.MapPost("/auth/register", Register)
               .WithName("Register")
               .WithOpenApi();
        }
        
        private static async Task<IResult> Login(LoginDto loginDto, IAuthService authService)
        {
            var authResponse = await authService.AuthenticateAsync(loginDto.Email, loginDto.Password);
            if (authResponse == null)
                return Results.Unauthorized();
            
            return Results.Ok(authResponse);
        }

        // for now just returns hashed password so we have to manually add to DB
        private static async Task<IResult> Register(RegisterDto registerDto, IAuthService authService)
        {
            var authResponse = await authService.RegisterAsync(registerDto.Email, registerDto.Password, registerDto.Name, registerDto.IsSuperAdmin);
            if (authResponse == null)
                return Results.BadRequest("User registration failed.");

            return Results.Ok(authResponse);
        }
    }
}