using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace APIs.Middleware
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst("userId");
                if (userIdClaim != null)
                {
                    // Store the userId in HttpContext.Items for later use in endpoints
                    context.Items["userId"] = userIdClaim.Value;
                }
            }
            
            await _next(context);
        }
    }
}
