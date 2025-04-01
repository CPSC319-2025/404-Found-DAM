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

        private static async Task<IResult> GetUser(IUserService userService, int userID, HttpContext context)
        {
            try
            {
                int requesterID = Convert.ToInt32(context.Items["userId"]);
                GetUserRes result = await userService.GetUser(requesterID);
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
