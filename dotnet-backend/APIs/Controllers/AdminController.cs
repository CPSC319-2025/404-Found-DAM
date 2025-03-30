using Core.Interfaces;
using Core.Dtos;
using Infrastructure.Exceptions;
using Microsoft.Net.Http.Headers;


// Use Task<T> or Task for async operations

namespace APIs.Controllers
{
    public static class AdminController
    {

        // TODO: replace mocked userID with authenticated userID
        private const int MOCKEDUSERID = 1;

        private const bool AdminActionTrue = true;

        private const bool logDebug = true;

        // private static IActivityLogService _activityLogService;
        // private static IProjectService _projectService;

        // private static IUserService _userService;

        // public static void Initialize(IActivityLogService activityLogService, IProjectService projectService, IUserService userService)
        // {
        //     _activityLogService = activityLogService;
        //     _projectService = projectService;
        //     _userService = userService;
        // }
        public static void MapAdminEndpoints(this WebApplication app)
        {
            // TODO: Mostly done; need to check user credentials:
        
            app.MapPatch("/projects/{projectID}/metadata/fields/{fieldID}", ToggleMetadataCategoryActivation).WithName("ToggleMetadataCategoryActivation").WithOpenApi();
            app.MapGet("/credentials/accounts/{userID}", GetRoleDetails).WithName("GetRoleDetails").WithOpenApi();
            app.MapPatch("/projects/{projectID}/accounts/{userID}/role", ModifyRole).WithName("ModifyRole").WithOpenApi();
            app.MapPost("/projects/{projectID}/metadata/fields", AddMetaDataFieldsToProject).WithName("AddMetaDataFieldsToProject").WithOpenApi();
            app.MapPost("/projects", CreateProjects).WithName("CreateProjects").WithOpenApi();
            app.MapGet("/users", GetAllUsers).WithName("GetUsers").WithOpenApi();
            app.MapPost("/projects/{projectID}/add-users", AddUsersToProject).WithName("AddUsersToProject").WithOpenApi();
            app.MapPatch("/projects/{projectID}/remove-users", DeleteUsersFromProject).WithName("DeleteUsersFromProject").WithOpenApi();
            app.MapPost("/projects/{projectID}/export", ExportProject).WithName("ExportProject").WithOpenApi();
            app.MapPost("/project/import", ImportProject).WithName("ImportProject").DisableAntiforgery();;

            // TODO: Not implemented yet
            // app.MapDelete("/projects", DeleteProjects).WithName("DeleteProjects").WithOpenApi();
            // app.MapPatch("/projects/{projectID}/permissions", UpdateProjectAccessControl).WithName("UpdateProjectAccessControl").WithOpenApi();
        }

        private static IServiceProvider GetServiceProvider(HttpContext context)
        {
            return context.RequestServices; // for activity log

        }

        private static async Task<IResult> ExportProject(int projectID, IAdminService adminService, HttpContext context)
        {

            
            try 
            {
                // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();
                // TODO: Check requester is a super admin in DB
                int requesterID = MOCKEDUSERID;

                // Get binary data of the Excel file containing details of the exported project
                (string fileName, byte[] excelData) = await adminService.ExportProject(projectID, requesterID);
                if (excelData == null || excelData.Length == 0)
                {
                    return Results.NotFound("No project is found to be exported");
                }
                else 
                {
                    var result  = Results.File(
                        excelData, 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",  // MIME type for Excel
                        fileDownloadName: fileName  // The filename for the download
                    );   

                    // Manually set Content-Disposition Header to instruct the browser to download the file  
                    context.Response.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName }.ToString();
                    var user = await userService.GetUser(requesterID);
                    string username = user.Name;
                    string theDescription = $"{username} (User ID: {requesterID}) exported project {fileName} (project id: {projectID})";
                    if (logDebug) {
                        theDescription += " [Add Log done by AdminController.ExportProject]";
                        Console.WriteLine(theDescription);
                    }
                    await activityLogService.AddLogAsync(new CreateActivityLogDto
                    {
                        // Add log (done)
                        userID = requesterID,
                        changeType = "Export",
                        description = theDescription,
                        projID = projectID, // would it be a problem (scope wise) if this was just called projectID (no "the")
                        assetID = "",
                        isAdminAction = AdminActionTrue
                    });
                    return result;
                }
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
                - Assume users are in the system (throw errors if some are not)
                - No need to import assets (i.e., a project with users only, no assets included yet), so just need to establish projectmemberships
                - The imported excel file extension is .xlsx, which is zipped.
                - The project Excel file must adhere to the format seen in the provided example, or the operation will fail and return an error to the user                       -  
        */
        private static async Task<IResult> ImportProject(IFormFile file , IAdminService adminService, HttpContext context)
        {
            try 
            {
                // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();

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

                    GetProjectRes project = result.importedProjectInfo;

                    var projectName = project.name;

                    var projectID = project.projectID;

                    // add log
                    var user = await userService.GetUser(MOCKEDUSERID);
                    string username = user.Name;
                    string theDescription = $"{username} (User ID: {MOCKEDUSERID}) imported project {projectName} (project ID: {projectID})";
                    if (logDebug) {
                        theDescription += "[Add Log called by AdminController.ImportProject]";
                        Console.WriteLine(theDescription);
                    }
                    await activityLogService.AddLogAsync(new CreateActivityLogDto
                    {
                        userID = MOCKEDUSERID,
                        changeType = "Import",
                        description = theDescription,
                        projID = projectID,
                        assetID = "",
                        isAdminAction = AdminActionTrue
                    });
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

        private static async Task<IResult> DeleteUsersFromProject(int projectID, DeleteUsersFromProjectReq req, IAdminService adminService, HttpContext context) 
        {
            try 
            {
                // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();

                if ((req.removeFromAdmins == null || req.removeFromAdmins.Count == 0) &&
                    (req.removeFromRegulars == null || req.removeFromRegulars.Count == 0))
                {
                    return Results.BadRequest("No users to be removed.");
                }
                else 
                {
                    int reqeusterID = MOCKEDUSERID;
                    DeleteUsersFromProjectRes result = await adminService.DeleteUsersFromProject(reqeusterID, projectID, req);

                    var project = await projectService.GetProject(projectID);
                    var projectName = project.name;

                    // add log (done)
                    var user = await userService.GetUser(reqeusterID);
                    string username = user.Name;
                    string removedUsers = string.Join(", ", 
                        (req.removeFromAdmins ?? new List<int>()).Concat(req.removeFromRegulars ?? new List<int>()));
                    string theDescription = $"{username} (User ID: {reqeusterID}) removed users ({removedUsers}) from project {projectName} (project ID: {projectID})";
                    // string description = $"Removed users: {removedUsers}";
                    if (logDebug) {
                        theDescription += "[Add Log called by AdminController.DeleteUsersFromProject]";
                        Console.WriteLine(theDescription);
                    }
                    await activityLogService.AddLogAsync(new CreateActivityLogDto
                    {
                        userID = reqeusterID,
                        changeType = "Remove Users",
                        description = theDescription,
                        projID = projectID,
                        assetID = "",
                        isAdminAction = AdminActionTrue
                    });
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

        private static async Task<IResult> AddUsersToProject(int projectID, AddUsersToProjectReq req, IAdminService adminService, HttpContext context) 
        {
            try 
            {

                // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();


                if ((req.addAsAdmin == null || req.addAsAdmin.Count == 0) &&
                    (req.addAsRegular == null || req.addAsRegular.Count == 0))
                {
                    return Results.BadRequest("No users to be added.");
                }
                else 
                {
                    int reqeusterID = MOCKEDUSERID; // TODO: replace with the actual requesterID from the token
                    AddUsersToProjectRes result = await adminService.AddUsersToProject(reqeusterID, projectID, req);

                    // add log (done)

                    var project = await projectService.GetProject(projectID);
                    var theProjectName = project.name;

                    var user = await userService.GetUser(reqeusterID);
                    string username = user.Name;

                    string addedUsers = string.Join(", ", 
                        (req.addAsAdmin ?? new List<int>()).Concat(req.addAsRegular ?? new List<int>()));
                    string theDescription = $"{username} (User ID: {reqeusterID}) added users ({addedUsers}) into project {theProjectName} (project ID: {projectID})";
                    // string description = $"Added users: {addedUsers}";
                    if (logDebug) {
                        theDescription += "[Add Log called by AdminController.AddUsersToProject]";
                        Console.WriteLine(theDescription);
                    }
                    await activityLogService.AddLogAsync(new CreateActivityLogDto
                    {
                        userID = reqeusterID,
                        changeType = "Add Users",
                        description = theDescription,
                        projID = projectID,
                        assetID = "",
                        isAdminAction = AdminActionTrue
                    });
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

        private static async Task<IResult> CreateProjects(List<CreateProjectsReq> req, IAdminService adminService, HttpContext context)
        {
            try 
            {

                // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();

                int theUserID = MOCKEDUSERID;
                List<CreateProjectsRes> result = await adminService.CreateProjects(req, theUserID);
                Console.WriteLine("here");

                foreach (var createProjectResEntry in result) 
                {
                    int theProjectID = createProjectResEntry.createdProjectID;
                    var getProjectDto = await projectService.GetProject(theProjectID);

                    Console.WriteLine("288");
                    var theProjectName = getProjectDto.name;



                    // Collect admin and user IDs
                    var adminIDs = req.SelectMany(r => r.admins ?? new List<int>()).ToList();
                    var userIDs = req.SelectMany(r => r.users ?? new List<int>()).ToList();

                    var adminDetails = await Task.WhenAll(adminIDs.Select(async adminID => 
                    {
                        var admin = await userService.GetUser(adminID);
                        return $"{admin.Name} (User ID: {adminID})";
                    }));

                    var userDetails = await Task.WhenAll(userIDs.Select(async userID => 
                    {
                        var user = await userService.GetUser(userID);
                        return $"{user.Name} (User ID: {userID})";
                    }));
                    Console.WriteLine("308");

                    string addedAdmins = string.Join(", ", adminDetails);
                    string addedUsers = string.Join(", ", userDetails);

                    Console.WriteLine("313");

                    var user = await userService.GetUser(theUserID);
                    Console.WriteLine("316");
                    string username = user.Name;
                    Console.WriteLine("318");

                    string theDescription = $"{username} (User ID: {theUserID}) created project {theProjectName} (Project ID: {theProjectID}) and added admins ({addedAdmins}) and users ({addedUsers}).";
                    // string addedUsers = string.Join(", ", userIDs);

                    // string theDescription = $"User {theUserID} created project {theProjectName} (Project ID: {theProjectID}) and added admins ({addedAdmins}) and users ({addedUsers}).";

                    if (logDebug) {
                        theDescription += "[Add Log called by AdminController.CreateProjects]";
                        Console.WriteLine(theDescription);
                    }

                    Console.WriteLine("theDescription: " + theDescription);

                    await activityLogService.AddLogAsync(new CreateActivityLogDto
                    {
                        userID = theUserID,
                        changeType = "Create",
                        description = theDescription,
                        projID = theProjectID,
                        assetID = "",
                        isAdminAction = AdminActionTrue
                    });
                }

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


        private static async Task<IResult> AddMetaDataFieldsToProject(int projectID, List<AddMetadataReq> req, IAdminService adminService, HttpContext context)
        {
            try 
            {
                if (logDebug) {
                    Console.WriteLine("AddMetaDataFieldsToProject - START");
                }
                // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();

                List<AddMetadataRes> result = await adminService.AddMetaDataFieldsToProject(projectID, req);
                // add log (done)
                var project = await projectService.GetProject(projectID);
                var theProjectName = project.name;
                string metadataDescriptions = string.Join(", ", req.Select(r => r.fieldName));
                // string description = $"Added metadata fields: {metadataDescriptions}";
                var user = await userService.GetUser(MOCKEDUSERID);
                string username = user.Name;

                var theDescription = $"{username} (User ID: {MOCKEDUSERID}) added metadata ({metadataDescriptions}) to project {theProjectName} (project ID: {projectID})";
                if (logDebug) {
                    theDescription += "[Add Log called by AdminController.AddMetaDataFieldsToProject]";
                    Console.WriteLine(theDescription);
                }

        
                await activityLogService.AddLogAsync(new CreateActivityLogDto
                {
                    userID = MOCKEDUSERID,
                    changeType = "Add Metadata",
                    description = theDescription,
                    projID = projectID,
                    assetID = "",
                    isAdminAction = AdminActionTrue
                });
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
            }         
        }

        private static async Task<IResult> ModifyRole(int projectID, int userID, ModifyRoleReq req, IAdminService adminService, HttpContext context)
        { 
            // Get services from IServiceProvider
            var serviceProvider = GetServiceProvider(context);
            var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
            var projectService = serviceProvider.GetRequiredService<IProjectService>();
            var userService = serviceProvider.GetRequiredService<IUserService>();
            
            string normalizedRoleString = req.roleChangeTo.Trim().ToLower();
            
            if (normalizedRoleString == "admin" || normalizedRoleString == "regular")
            {
                try 
                {
                    ModifyRoleRes result = await adminService.ModifyRole(projectID, userID, normalizedRoleString);
                    // add log (done)
                    // ModifyRoleRes result = await adminService.ModifyRole(projectID, userID, normalizedRoleString);

                    var user = await userService.GetUser(MOCKEDUSERID);
                    var username = user.Name;
                    var projectName = await projectService.GetProjectNameByIdAsync(projectID);
                    var theDescription = $"{username} (User ID: {MOCKEDUSERID}) changed role of user {username} (User ID: {userID}) to {normalizedRoleString} in {projectName} (Project ID: {projectID})";

                    if (logDebug) {
                        theDescription += "[Add Log called by AdminController.ModifyRole]";
                        Console.WriteLine(theDescription);
                    }
                    await activityLogService.AddLogAsync(new CreateActivityLogDto
                    {
                        userID = MOCKEDUSERID,
                        changeType = "Modify Role",
                        description = theDescription,
                        projID = projectID,
                        assetID = "",
                        isAdminAction = AdminActionTrue
                    });
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

        private static async Task<IResult> ToggleMetadataCategoryActivation(int projectID, int fieldID, ToggleMetadataStateReq req, IAdminService adminService, HttpContext context)
        {

            try 
            {

                // Get services from IServiceProvider
                var serviceProvider = GetServiceProvider(context);
                var activityLogService = serviceProvider.GetRequiredService<IActivityLogService>();
                var projectService = serviceProvider.GetRequiredService<IProjectService>();
                var userService = serviceProvider.GetRequiredService<IUserService>();
                ToggleMetadataStateRes result = await adminService.ToggleMetadataCategoryActivation(projectID, fieldID, req.enabled);

                var project = await projectService.GetProject(projectID);
                var projectName = project.name;
                var user = await userService.GetUser(MOCKEDUSERID);
                var username = user.Name;
                var theDescription = $"{username} (User ID: {MOCKEDUSERID}) toggled metadata field {fieldID} to {(req.enabled ? "enabled" : "disabled")} for project {projectName} (project ID: {projectID})";
                if (logDebug) {
                    theDescription += "[Add Log called by AdminController.ToggleMetadataCategoryActivation]";
                    Console.WriteLine(theDescription);
                }
                // add log (done)
                await activityLogService.AddLogAsync(new CreateActivityLogDto
                {
                    userID = MOCKEDUSERID,
                    changeType = "Toggle Metadata Activation",
                    description = theDescription,
                    projID = projectID,
                    assetID = "",
                    isAdminAction = AdminActionTrue
                });

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

        private static async Task<IResult> GetAllUsers(IAdminService adminService) 
        {
            try
            {
                //TODO: replace MOCKUSERID with authenticated userID
                int userID = MOCKEDUSERID;
                GetAllUsersRes result = await adminService.GetAllUsers(userID);
                return Results.Ok(result);
            }
            catch (DataNotFoundException ex) {
                return Results.NotFound(ex.Message);
            }
            catch (Exception) {
                return Results.StatusCode(500);
            }
        }
        
    }
}
