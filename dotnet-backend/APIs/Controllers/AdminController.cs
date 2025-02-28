using Core.Interfaces;
using Core.Dtos;

// Use Task<T> or Task for async operations

namespace APIs.Controllers
{
    public static class AdminController
    {
        public static void MapAdminEndpoints(this WebApplication app)
        {
            app.MapGet("/credentials/accounts/{userId}", GetRoleDetails).WithName("GetRoleDetails").WithOpenApi();
  
            app.MapPost("/projects/{projectId}/metadata", CreateProject).WithName("CreateProject").WithOpenApi();
           
            app.MapPost("/projects/{projectID}/metadata/fields", AddMetaDataToProject).WithName("AddMetaDataToProject").WithOpenApi();

            app.MapPut("/projects/{projectId}/accounts/{userId}/role", ModifyRole).WithName("ModifyRole").WithOpenApi();

            app.MapPatch("/projects/{projectID}/metadata/fields/{fieldID}", ToggleMetadataCategoryActivation).WithName("ToggleMetadataCategoryActivation").WithOpenApi();

            app.MapPatch("/projects/{projectID}/permissions", UpdateProjectAccessControl).WithName("UpdateProjectAccessControl").WithOpenApi();

        }

        private static IResult GetRoleDetails(string userId, ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult CreateProject(CreateProjectReq req, ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult AddMetaDataToProject(AddMetadataReq req, ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult ModifyRole(ModifyRoleReq req, ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult ToggleMetadataCategoryActivation(ToggleMetadataStateRequest req, ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult UpdateProjectAccessControl(UpdateProjectAcessRequest req, ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }
    }
}
