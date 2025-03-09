using Core.Interfaces;
using Core.Dtos;
using Infrastructure.Exceptions;

// Use Task<T> or Task for async operations

namespace APIs.Controllers
{
    public static class AdminController
    {
        public static void MapAdminEndpoints(this WebApplication app)
        {
            // TODO: Mostly done; need to check user credentials:
            app.MapPatch("/projects/{projectID}/metadata/fields/{fieldID}", ToggleMetadataCategoryActivation).WithName("ToggleMetadataCategoryActivation").WithOpenApi();

            // TODO: Return mocked data currently:
            app.MapGet("/credentials/accounts/{userID}", GetRoleDetails).WithName("GetRoleDetails").WithOpenApi();
  
            // TODO: Not implemented yet
            // app.MapPost("/projects/{projectID}/metadata", CreateProject).WithName("CreateProject").WithOpenApi();
            // app.MapPost("/projects/{projectID}/metadata/fields", AddMetaDataToProject).WithName("AddMetaDataToProject").WithOpenApi();
            // app.MapPut("/projects/{projectID}/accounts/{userId}/role", ModifyRole).WithName("ModifyRole").WithOpenApi();
            // app.MapPatch("/projects/{projectID}/permissions", UpdateProjectAccessControl).WithName("UpdateProjectAccessControl").WithOpenApi();
        }

        private static async Task<IResult> GetRoleDetails(int userID, IAdminService adminService)
        {
            try 
            {
                RoleDetailsRes result = await adminService.GetRoleDetails(userID);
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
           catch (DataNotFoundException ex) 
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception) 
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
