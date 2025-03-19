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
        }
        
        private static async Task<IResult> Login(LoginDto loginDto, IAuthService authService)
        {
            var authResponse = await authService.AuthenticateAsync(loginDto.Email, loginDto.Password);
            if (authResponse == null)
                return Results.Unauthorized("Invalid credentials");
            
            return Results.Ok(authResponse);
        }
    }
}
