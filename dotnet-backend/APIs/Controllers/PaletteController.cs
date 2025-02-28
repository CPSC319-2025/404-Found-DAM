using Core.Interfaces;
using Core.Dtos;

// Use Task<T> or Task for async operations

namespace APIs.Controllers
{
    public static class PaletteController
    {

        // PUT /palette/assets/{assetId} edit asset in the pallete
        // DELETE /projects/assign-assets  delete an asset from palette

        public static void MapPaletteEndpoints(this WebApplication app)
        {
            // assets already in the pallete
            app.MapGet("/palette/assets", GetPaletteAssets).WithName("GetPaletteAssets").WithOpenApi();

            // tag assets in the palette
            app.MapPost("/projects/assign-assets", AssignAssetsToProjects).WithName("AssignAssetsToProjects").WithOpenApi();

            // choose a project for an asset
            app.MapPost("/projects/{projectId}/assets/tags", AssignTagsToAssets).WithName("AssignTagsToAssets").WithOpenApi();
           
           // upload assets permanently
            app.MapPut("/projects/upload-assets", UploadAssets).WithName("UploadAssets").WithOpenApi();

            // delete/add tags
            app.MapPatch("/palette/assets/{assetId}/tags", ModifyTags).WithName("ModifyTags").WithOpenApi();

        }

        private static IResult GetPaletteAssets(GetPaletteAssetsReq req, ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult AssignAssetsToProjects(AssignAssetsToProjectsReq req, ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult AssignTagsToAssets(AssignTagsToAssetsReq req, ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult UploadAssets(UploadAssetsReq req, ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }

        private static IResult ModifyTags(ModifyTagsReq req, ITestService projectService)
        {
            return Results.NotFound("stub"); // Stub
        }


    }
}