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

        public SearchRepository(DAMDbContext context) {
            _context = context;
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

        public async Task<List<Asset>> SearchAssetsAsync(string query) 
        {
            return await _context.Assets
                .Include(a => a.AssetTags)
                    .ThenInclude(at => at.Tag)
                .Include(a => a.AssetMetadata)
                .Include(a => a.Project)
                .Where(a => 
                EF.Functions.Like(a.FileName, $"%{query}%") ||
                a.AssetTags.Any(at => EF.Functions.Like(at.Tag.Name, $"%{query}%")) ||
                a.AssetMetadata.Any(am => EF.Functions.Like(am.FieldValue, $"%{query}%")))
                .ToListAsync();
        }
    }
}