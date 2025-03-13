using Core.Interfaces;
using Core.Dtos;
using Infrastructure.Exceptions;

// Use Task<T> or Task for async operations

namespace APIs.Controllers
{
    public static class AdminController
    {

        // TODO: replace mocked userID with authenticated userID
        private const int MOCKEDUSERID = 1;
        public static void MapAdminEndpoints(this WebApplication app)
        {
            // TODO: Mostly done; need to check user credentials:
            app.MapPatch("/projects/{projectID}/metadata/fields/{fieldID}", ToggleMetadataCategoryActivation).WithName("ToggleMetadataCategoryActivation").WithOpenApi();
            app.MapGet("/credentials/accounts/{userID}", GetRoleDetails).WithName("GetRoleDetails").WithOpenApi();
            app.MapPatch("/projects/{projectID}/accounts/{userID}/role", ModifyRole).WithName("ModifyRole").WithOpenApi();
            app.MapPost("/projects/{projectID}/metadata/fields", AddMetaDataFieldsToProject).WithName("AddMetaDataFieldsToProject").WithOpenApi();
            app.MapPost("/projects", CreateProjects).WithName("CreateProjects").WithOpenApi();


            // TODO: Not implemented yet
            // app.MapPatch("/projects/{projectID}/permissions", UpdateProjectAccessControl).WithName("UpdateProjectAccessControl").WithOpenApi();
            // Add user(s) to project; check if project exists, if so then check if user exists in the system, and if user is a member of this project and if req is complete before adding!
            // remove user(s) from project; check if project exists, if so retrieve users and check if they exists in the system before removing!
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

        private static async Task<IResult> CreateProjects(List<CreateProjectsReq> req, IAdminService adminService)
        {
            try 
            {
                int userID = MOCKEDUSERID;
                List<CreateProjectsRes> result = await adminService.CreateProjects(req, userID);
                return Results.Ok(result); 
            }
            catch (DataNotFoundException ex) 
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return Results.Problem
                (
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal Server Error"
                );
            }      
        }

        private static async Task<IResult> AddMetaDataFieldsToProject(int projectID, List<AddMetadataReq> req, IAdminService adminService)
        {
            try 
            {
                List<AddMetadataRes> result = await adminService.AddMetaDataFieldsToProject(projectID, req);
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
