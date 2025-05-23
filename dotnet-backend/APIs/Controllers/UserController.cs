using Core.Interfaces;
using Core.Dtos;
using Infrastructure.Exceptions;

// Use Task<T> or Task for async operations

namespace APIs.Controllers
{
    public static class UserController
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            app.MapGet("/users/{userID}", GetUser).WithName("GetUser").WithOpenApi();
        }

        private static async Task<IResult> GetUser(IUserService userService, int userID, HttpContext context) //userID is the userID we are trying to lookup. It is not the person who is making the call.
        {
            try
            {
                GetUserRes result = await userService.GetUser(userID);
                return Results.Ok(result);
            }
            catch (DataNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception)
            {
                return Results.StatusCode(500);
            }
        }
    }
}
