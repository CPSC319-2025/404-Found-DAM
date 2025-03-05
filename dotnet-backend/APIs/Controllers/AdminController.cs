using Core.Interfaces;
using Core.Dtos;

// Use Task<T> or Task for async operations

namespace APIs.Controllers
{
    public static class AdminController
    {
        public static void MapAdminEndpoints(this WebApplication app)
        {

            app.MapPatch("/projects/{projectID}/metadata/fields/{fieldID}", ToggleMetadataCategoryActivation).WithName("ToggleMetadataCategoryActivation").WithOpenApi();

            app.MapGet("/credentials/accounts/{userID}", GetRoleDetails).WithName("GetRoleDetails").WithOpenApi();
  
            // app.MapPost("/projects/{projectId}/metadata", CreateProject).WithName("CreateProject").WithOpenApi();
           
            // app.MapPost("/projects/{projectID}/metadata/fields", AddMetaDataToProject).WithName("AddMetaDataToProject").WithOpenApi();

            // app.MapPut("/projects/{projectId}/accounts/{userId}/role", ModifyRole).WithName("ModifyRole").WithOpenApi();

            // app.MapPatch("/projects/{projectID}/permissions", UpdateProjectAccessControl).WithName("UpdateProjectAccessControl").WithOpenApi();

        }

        private static async Task<IResult> GetRoleDetails(int userId, IAdminService adminService)
        {
            try 
            {
                RoleDetailsRes result = await adminService.GetRoleDetails(userId);
                return result == null ? Results.NotFound("No user was found.") : Results.Ok(result); 
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
        }

        private static IResult CreateProject(CreateProjectReq req, IAdminService adminService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult AddMetaDataToProject(AddMetadataReq req, IAdminService adminService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult ModifyRole(ModifyRoleReq req, IAdminService adminService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static async Task<IResult> ToggleMetadataCategoryActivation(int projectId, int fieldId, ToggleMetadataStateReq req, IAdminService adminService)
        {
            try 
            {
                ToggleMetadataStateRes result = await adminService.ToggleMetadataCategoryActivation(projectId, fieldId, req.enabled);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
        }

        private static IResult UpdateProjectAccessControl(UpdateProjectAcessRequest req, IAdminService adminService)
        {
            return Results.NotFound("stub"); // Stub
        }
    }
}
