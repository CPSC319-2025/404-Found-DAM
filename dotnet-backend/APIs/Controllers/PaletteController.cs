using Core.Interfaces;
using Core.Dtos;

namespace APIs.Controllers
{
    public static class PaletteController
    {

        // PUT /palette/assets/{assetId} edit asset in the pallete
        // DELETE /projects/assign-assets  delete an asset from palette

        public static void MapPaletteEndpoints(this WebApplication app)
        {
            // assets already in the pallete
        app.MapGet("/palette/assets", () => 
        {
            return Results.NotFound("stub"); // Stub
            // return GetPaletteAssets(req);
        })
        .WithName("GetPaletteAssets")
        .WithOpenApi();

        app.MapPost("/palette/upload", async (HttpRequest request, IPaletteService paletteService) =>
        {
            return await UploadAssets(request, paletteService);
        })
        .WithName("UploadAssets")
        .WithOpenApi();


        //     // tag assets in the palette
        //     app.MapPost("/projects/assign-assets", AssignAssetsToProjects).WithName("AssignAssetsToProjects").WithOpenApi();

        //     // choose a project for an asset
        //     app.MapPost("/projects/{projectId}/assets/tags", AssignTagsToAssets).WithName("AssignTagsToAssets").WithOpenApi();
           
        //    // upload assets permanently
        //     app.MapPost("/projects/upload-assets", UploadAssets).WithName("UploadAssets").WithOpenApi();

        //     // delete/add tags
        //     app.MapPatch("/palette/assets/{assetId}/tags", ModifyTags).WithName("ModifyTags").WithOpenApi();

        }


        // TODO: Bring back ITestService projectService
        private static async Task<IResult> UploadAssets(HttpRequest request, IPaletteService paletteService)
        {
            // Check if the request has form data
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                return Results.BadRequest("No files uploaded");
            }

            // Get the form fields that match your DTO
            string name = request.Form["Name"].ToString();
            string type = request.Form["Type"].ToString();
            int userId = int.Parse(request.Form["UserId"].ToString());

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type))
            {
                return Results.BadRequest("Name and Type are required");
            }

            // Create your DTO
            var uploadRequest = new UploadAssetsReq
            {
                Name = name,
                Type = type,
                UserId = userId
            };

            // Create a task for each file
            var uploadTasks = request.Form.Files.Select(async file => 
            {
                var res = await paletteService.ProcessUploadAsync(file, uploadRequest);
                if (res){
                    return new { 
                        Success = true, 
                        FileName = file.FileName, 
                        Size = file.Length
                    };
                }
                else {
                    return new { 
                        Success = false, 
                        FileName = file.FileName, 
                        Size = file.Length
                    };
                }
            }).ToList();

            // Wait for all tasks to complete
            var results = await Task.WhenAll(uploadTasks);

            // Check if any uploads failed
            var failures = results.Where(r => !r.Success).ToList();
            
            if (failures.Count == results.Length)
            {
                return Results.BadRequest(failures);
            }
            
            // Return combined results
            return Results.Ok(new {
                SuccessCount = results.Count(r => r.Success),
                FailureCount = failures.Count,
                Details = results
            });
        }
        
    }
}