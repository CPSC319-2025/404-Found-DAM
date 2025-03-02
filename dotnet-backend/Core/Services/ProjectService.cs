using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Interfaces;
using Core.Dtos;

namespace Core.Services
{
    public class ProjectService : IProjectService
    {
        public async Task<GetProjectAssetsRes> GetProjectAssets(string projectId, string type, int pageNumber, int pageSize)
        {
            int offset = (pageNumber - 1) * pageSize;
            // var retrievedAssets = GetAssetsFromDatabase(offset, pageSize);
            ProjectAssetsPagination pagination = new ProjectAssetsPagination{page = pageNumber, limit = pageSize, total = 2};
            List<string> tags1 = new List<string>();
            tags1.Add("fieldwork");
            tags1.Add("site");
            ProjectAssetMd metadata1 = new ProjectAssetMd{date = "2025-01-30T10:20:00Z", tags = tags1};

            List<string> tags2 = new List<string>();
            tags2.Add("inspection");
            ProjectAssetMd metadata2 = new ProjectAssetMd{date =  "2025-01-30T10:25:00Z", tags = tags2};

            ProjectAsset asset1 = new ProjectAsset
            {
                id = "img001", 
                thumbnailUrl = "https://cdn.example.com/thumbnails/img001.jpg",
                filename = "image1.jpg",
                projectAssetMd = metadata1
            };

            ProjectAsset asset2 = new ProjectAsset
            {
                id = "img002", 
                thumbnailUrl = "https://cdn.example.com/thumbnails/img002.jpg",
                filename = "image2.jpg",
                projectAssetMd = metadata2
            };

            List<ProjectAsset> assets = new List<ProjectAsset>();
            assets.Add(asset1);
            assets.Add(asset2);

            GetProjectAssetsRes result = new GetProjectAssetsRes{projectId = projectId, assets = assets, pagination = pagination};
            return result;
        }
    }
}
