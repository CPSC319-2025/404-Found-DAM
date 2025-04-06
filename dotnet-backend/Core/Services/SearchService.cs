using Core.Dtos;         
using Core.Entities;      
using Core.Interfaces; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Services
{
    public class SearchService : ISearchService
    {
        private readonly ISearchRepository _searchRepository;

        public SearchService(ISearchRepository searchRepository) 
        {
            _searchRepository = searchRepository;
        }

        public async Task<SearchResultDto> SearchAsync(string query)
        {
            try
            {
                List<Project> matchingProjects = await _searchRepository.SearchProjectsAsync(query);
                var (matchingAssets, sasUrlMap) = await _searchRepository.SearchAssetsAsync(query);

                var projectResults = matchingProjects.Select(p => new GetProjectRes
                {
                    projectID = p.ProjectID,
                    name = p.Name,
                    description = p.Description,
                    location = p.Location,
                    active = p.Active,
                    archivedAt = null, // TODO: implement later
                    admins = null, // Don't need this
                    regularUsers = null, // Don't need this
                    tags = null // Don't need this
                }).ToList();

                var assetResults = matchingAssets.Select(a => new AssetSearchResultDto
                {
                    blobID = a.BlobID,
                    filename = a.FileName,
                    tags = a.AssetTags.Select(t => t.Tag.Name).ToList(),
                    mimetype = a.MimeType,
                    filesizeInKB = a.FileSizeInKB,
                    uploadedBy = new AssetSearchResultUploadedBy
                    {
                        userID = a.User?.UserID ?? -1,
                        name = a.User?.Name ?? "Unknown",
                        email = a.User?.Email ?? "Unknown",
                    },
                    projectID = a.Project != null ? a.Project.ProjectID : 0,
                    projectName = a.Project != null ? a.Project.Name : "Unknown",
                    BlobSASUrl = sasUrlMap.TryGetValue(a.BlobID, out var url) ? url : null
                }).ToList();

                return new SearchResultDto
                {
                    projects = projectResults,
                    assets = assetResults
                };
            }
            catch (Exception e)
            {
                throw; // This catch block isn't doing anything, consider logging `e`
            }
        }
    }
}