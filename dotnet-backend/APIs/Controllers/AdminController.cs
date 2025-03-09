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
            app.MapGet("/credentials/accounts/{userID}", GetRoleDetails).WithName("GetRoleDetails").WithOpenApi();
            app.MapPatch("/projects/{projectID}/accounts/{userID}/role", ModifyRole).WithName("ModifyRole").WithOpenApi();
            app.MapPost("/projects/{projectID}/metadata/fields", AddMetaDataToProject).WithName("AddMetaDataToProject").WithOpenApi();


            // TODO: Not implemented yet
            // app.MapPost("/projects/{projectID}/metadata", CreateProject).WithName("CreateProject").WithOpenApi();
            // app.MapPost("/projects/{projectID}/metadata/fields", AddMetaDataToProject).WithName("AddMetaDataToProject").WithOpenApi();
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

        private static async Task<IResult> CreateProject(CreateProjectReq req, IAdminService adminService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static async Task<IResult> AddMetaDataToProject(int projectID, AddMetadataReq req, IAdminService adminService)
        {
            try 
            {
                AddMetadataRes result = await adminService.AddMetaDataToProject(projectID, req.fieldName, req.fieldType);
                return Results.Ok(result); 
            }
            catch (ArgumentException ex) 
            {
                return Results.BadRequest(ex.Message);
            }
            catch (DataNotFoundException ex) 
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception) 
            {
                return Results.StatusCode(500);
            }          }

        private static async Task<IResult> ModifyRole(int projectID, int userID, ModifyRoleReq req, IAdminService adminService)
        {
            try 
            {
                ModifyRoleRes result = await adminService.ModifyRole(projectID, userID, req.userToAdmin);
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

        private static async Task<IResult> ToggleMetadataCategoryActivation(int projectID, int fieldID, ToggleMetadataStateReq req, IAdminService adminService)
        {
            try 
            {
                ToggleMetadataStateRes result = await adminService.ToggleMetadataCategoryActivation(projectID, fieldID, req.enabled);
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
