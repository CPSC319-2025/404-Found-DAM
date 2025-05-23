using Core.Interfaces;
using Core.Dtos;

// Use Task<T> or Task for async operations

namespace APIs.Controllers
{
    public static class NotificationController
    {
        public static void MapNotificationEndpoints(this WebApplication app)
        {
            // app.MapGet("/notifications", RetrieveNotification).WithName("RetrieveNotification").WithOpenApi();
            // app.MapPost("/notifications/send", SendNotifications).WithName("SendNotifications").WithOpenApi();
            // app.MapPost("/notifications/status", SendStatusUpdate).WithName("SendStatusUpdate").WithOpenApi();
        }

        private static IResult RetrieveNotification(string userId)
        {
            return Results.NotFound("stub"); // Stub

        }

        private static IResult SendNotifications(SendNotificationReq req)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult SendStatusUpdate(SendStatusUpdateReq req)
        {
            return Results.NotFound("stub"); // Stub
        }
    }
}
