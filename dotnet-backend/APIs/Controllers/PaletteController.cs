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
        
        // update the images in the palette with the selected project tags
        app.MapPatch("/palette/images/tags", async (AssignTagsToPaletteReq request, IPaletteService paletteService, ILogger<Program> logger) => 
        {
            var result = await paletteService.AddTagsToPaletteImagesAsync(request.ImageIds, request.ProjectId);
            if (result) {
                return Results.Ok(new {
                    status = "success",
                    projectId = request.ProjectId,
                    updatedImages = request.ImageIds,
                    message = "Tags successfully added to selected images in the palette."
                });
            } else {
                Console.WriteLine($"Failed to assign project tags to images for ProjectId {result}.");
                return Results.NotFound("Failed to assign project tags to images");
            }
        })
        .WithName("ModifyTags")
        .WithOpenApi();


        //     // tag assets in the palette
        //     app.MapPost("/projects/assign-assets", AssignAssetsToProjects).WithName("AssignAssetsToProjects").WithOpenApi();

        //     // choose a project for an asset
        //     app.MapPost("/projects/{projectId}/assets/tags", AssignTagsToAssets).WithName("AssignTagsToAssets").WithOpenApi();
           
        //    // upload assets permanently
        //     app.MapPost("/projects/upload-assets", UploadAssets).WithName("UploadAssets").WithOpenApi();

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
            var results = await paletteService.ProcessUploadsAsync(request.Form.Files.ToList(), uploadRequest);
        
            // Return combined results
            return Results.Ok(results);
        }
        
    }
}