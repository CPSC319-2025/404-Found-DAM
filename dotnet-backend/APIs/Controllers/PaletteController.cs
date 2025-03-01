using Core.Dtos;
using Microsoft.AspNetCore.Mvc; 

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
        app.MapGet("/palette/assets", () => 
        {
            return Results.NotFound("stub"); // Stub
            // return GetPaletteAssets(req);
        })
        .WithName("GetPaletteAssets")
        .WithOpenApi();
        //     // tag assets in the palette
        //     app.MapPost("/projects/assign-assets", AssignAssetsToProjects).WithName("AssignAssetsToProjects").WithOpenApi();

        //     // choose a project for an asset
        //     app.MapPost("/projects/{projectId}/assets/tags", AssignTagsToAssets).WithName("AssignTagsToAssets").WithOpenApi();
           
        //    // upload assets permanently
        //     app.MapPut("/projects/upload-assets", UploadAssets).WithName("UploadAssets").WithOpenApi();

        //     // delete/add tags
        //     app.MapPatch("/palette/assets/{assetId}/tags", ModifyTags).WithName("ModifyTags").WithOpenApi();

        }

        // TODO: Bring back ITestService projectService 
        private static IResult GetPaletteAssets([AsParameters] GetPaletteAssetsReq req)
        {
            return Results.NotFound("stub"); // Stub
        }
        // TODO: Bring back ITestService projectService
        // private static IResult AssignAssetsToProjects([FromQuery] AssignAssetsToProjectsReq req)
        // {
        //     return Results.NotFound("stub"); // Stub
        // }
        // // TODO: Bring back ITestService projectService
        // private static IResult AssignTagsToAssets([FromQuery] AssignTagsToAssetsReq req)
        // {
        //     return Results.NotFound("stub"); // Stub
        // }
        // // TODO: Bring back ITestService projectService
        // private static IResult UploadAssets([FromQuery] UploadAssetsReq req)
        // {
        //     return Results.NotFound("stub"); // Stub
        // }
        // // TODO: Bring back ITestService projectService
        // private static IResult ModifyTags([FromQuery] ModifyTagsReq req)
        // {
        //     return Results.NotFound("stub"); // Stub
        // }

    }
}