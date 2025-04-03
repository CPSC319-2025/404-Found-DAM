using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.DataAccess
{
    public class SearchRepository : ISearchRepository
    {
        private readonly DAMDbContext _context;
        private readonly IBlobStorageService _blobStorageService;

        public SearchRepository(DAMDbContext context, IBlobStorageService blobStorageService) {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        public async Task<List<Project>> SearchProjectsAsync(string query)
        {
            // Querying projects by either project name or by the associated tag name
            return await _context.Projects
                .Include(p => p.ProjectTags)
                    .ThenInclude(pt => pt.Tag)
                .Include(p => p.Assets)
                    .ThenInclude(a => a.AssetMetadata)
                .Where(p => 
                    EF.Functions.Like(p.Name, $"%{query}%") || 
                    EF.Functions.Like(p.Description, $"%{query}%") ||
                    p.ProjectTags.Any(pt => EF.Functions.Like(pt.Tag.Name, $"%{query}%")) ||
                    p.Assets.Any(a => a.AssetMetadata.Any(am => EF.Functions.Like(am.FieldValue, $"%{query}%"))))
                .ToListAsync();
        }
        
        public async Task<(List<Asset>, Dictionary<string, string>)> SearchAssetsAsync(string query) 
        {
            var assets = await _context.Assets
                .Include(a => a.AssetTags)
                    .ThenInclude(at => at.Tag)
                .Include(a => a.AssetMetadata)
                .Include(a => a.Project)
                .Where(a => 
                    EF.Functions.Like(a.FileName, $"%{query}%") ||
                    a.AssetTags.Any(at => EF.Functions.Like(at.Tag.Name, $"%{query}%")) ||
                    a.AssetMetadata.Any(am => EF.Functions.Like(am.FieldValue, $"%{query}%")))
                .ToListAsync();
            
            var sasUrlMap = new Dictionary<string, string>();

            var groupedAssets = assets
                .Where(a => a.Project != null)
                .GroupBy(a => a.Project!.ProjectID)
                .ToList();

            var downloadTasks = new List<Task<(string container, List<(string, string)> assets, List<string> urls)>>();

            foreach (var projectGroup in groupedAssets)
            {
                string containerName = $"project-{projectGroup.Key}-assets";
                var assetIdNameTuples = projectGroup
                    .Select(a => (a.BlobID, a.FileName))
                    .ToList();

                var task = DownloadWithContainerName(containerName, assetIdNameTuples);
                downloadTasks.Add(task);
            }

            var results = await Task.WhenAll(downloadTasks);

            foreach (var (container, assetIdNameTuples, sasUrls) in results)
            {
                for (int i = 0; i < assetIdNameTuples.Count; i++)
                {
                    sasUrlMap[assetIdNameTuples[i].Item1] = sasUrls[i];
                }
            }

            return (assets, sasUrlMap);
        }

        private async Task<(string container, List<(string, string)> assets, List<string> urls)> 
        DownloadWithContainerName(string containerName, List<(string, string)> assetIdNameTuples)
        {
            var sasUrls = await _blobStorageService.DownloadAsync(containerName, assetIdNameTuples);
            return (containerName, assetIdNameTuples, sasUrls);
        }
    }
}