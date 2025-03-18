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
                List<Asset> matchingAssets = await _searchRepository.SearchAssetsAsync(query);

                var projectResults = matchingProjects.Select(p => new GetProjectRes
                {
                    projectID = p.ProjectID,
                    name = p.Name,
                    description = p.Description,
                    location = p.Location,
                    archived = !p.Active,
                    archivedAt = null, //TODO: implement later
                    admins = null, // dont need this
                    regularUsers = null, // dont need this
                    tags = null // dont need this
                    // tags = p.ProjectTags
                    //     .Select(pt => new TagCustomInfo
                    //     {
                    //         tagID = pt.Tag.TagID,
                    //         name = pt.Tag.Name
                    //     })
                    //     .ToList()
                }).ToList();

                var assetResults = matchingAssets.Select(a => new AssetSearchResultDto
                {
                    blobID = a.BlobID,
                    fileName = a.FileName,
                    thumbnailUrl = a.FileName,
                    tags = a.AssetTags.Select(at => at.Tag.Name).ToList(),
                    projectID = a.Project != null ? a.Project.ProjectID : 0,
                    projectName = a.Project != null ? a.Project.Name : "Unknown"
                }).ToList();

                return new SearchResultDto
                {
                    projects = projectResults,
                    assets = assetResults
                };
            }
            catch (Exception e) {
                throw;
            }
        }
    }
}