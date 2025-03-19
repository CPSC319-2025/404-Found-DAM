using Core.Interfaces;
using Core.Dtos;
using Infrastructure.Exceptions;
using Core.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using Microsoft.AspNetCore.Http;

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
            app.MapPost("/projects/{projectID}/add-users", AddUsersToProject).WithName("AddUsersToProject").WithOpenApi();
            app.MapPatch("/projects/{projectID}/remove-users", DeleteUsersFromProject).WithName("DeleteUsersFromProject").WithOpenApi();
            app.MapPost("/projects/{projectID}/export", ExportProject).WithName("ExportProject").WithOpenApi();
            app.MapPost("/project/import", ImportProject).WithName("ImportProject").DisableAntiforgery();;

            // TODO: Not implemented yet
            // app.MapDelete("/projects", DeleteProjects).WithName("DeleteProjects").WithOpenApi();
            // app.MapPatch("/projects/{projectID}/permissions", UpdateProjectAccessControl).WithName("UpdateProjectAccessControl").WithOpenApi();
        }

        private static async Task<IResult> ExportProject(int projectID, IAdminService adminService)
        {
            try 
            {
                // TODO: Check requester is a super admin in DB
                int requesterID = MOCKEDUSERID;

                // Get binary data of the Excel file containing details of the exported project
                (string fileName, byte[] excelData) = await adminService.ExportProject(projectID, requesterID);
                return excelData == null 
                    ? Results.NotFound("No project is found to be exported") 
                    : Results.File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName); // Return the Excel file's binary data
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

        /*
            ImportProject Assumes:
                - The imported excel file extension is .xlsx
                - Project, assets, and users do NOT exist in DB, so the IDs are omitted.
                    - Also assume asset has no custom metadata fields and values.
                - Relation between user and assets are not preserved in the import file.
            BlobID in the file of "import project example" is asset's local file path
        */
        private static async Task<IResult> ImportProject(IFormFile file , IAdminService adminService)
        {
            try 
            {
                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest("Empty file");
                }
                else
                {                
                    // TODO: Check the requester is a super amdin in the DB 
                    using var memoryStream = new MemoryStream();   
                    await file.CopyToAsync(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin); // Reset the memory stream's position.
                    ImportProjectRes result = await adminService.ImportProject(memoryStream);
                    return Results.Ok(result);
                }
            }
            catch (InvalidDataException ex)
            {
                return Results.BadRequest(ex.Message);
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

        private static async Task<IResult> DeleteUsersFromProject(int projectID, DeleteUsersFromProjectReq req, IAdminService adminService) 
        {
            try 
            {
                if ((req.removeFromAdmins == null || req.removeFromAdmins.Count == 0) &&
                    (req.removeFromRegulars == null || req.removeFromRegulars.Count == 0))
                {
                    return Results.BadRequest("No users to be removed.");
                }
                else 
                {
                    int reqeusterID = MOCKEDUSERID; // TODO: replace with the actual requesterID from the token
                    DeleteUsersFromProjectRes result = await adminService.DeleteUsersFromProject(reqeusterID, projectID, req);
                    return Results.Ok(result);
                }
            }
            catch (DataNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Results.StatusCode(500);
            }            
        }

        private static async Task<IResult> AddUsersToProject(int projectID, AddUsersToProjectReq req, IAdminService adminService) 
        {
            try 
            {
                if ((req.addAsAdmin == null || req.addAsAdmin.Count == 0) &&
                    (req.addAsRegular == null || req.addAsRegular.Count == 0))
                {
                    return Results.BadRequest("No users to be added.");
                }
                else 
                {
                    int reqeusterID = MOCKEDUSERID; // TODO: replace with the actual requesterID from the token
                    AddUsersToProjectRes result = await adminService.AddUsersToProject(reqeusterID, projectID, req);
                    return Results.Ok(result);
                }
            }
            catch (DataNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Results.StatusCode(500);
            }            
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
            string normalizedRoleString = req.roleChangeTo.Trim().ToLower();
            
            if (normalizedRoleString == "admin" || normalizedRoleString == "regular")
            {
                try 
                {
                    ModifyRoleRes result = await adminService.ModifyRole(projectID, userID, normalizedRoleString);
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
            else 
            {                
                return Results.BadRequest("roleChangeTo must be either \"admin\" or \"regular\"");
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
