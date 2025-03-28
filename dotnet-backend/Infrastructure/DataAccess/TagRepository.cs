using Core.Dtos;
using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess
{
    public class TagRepository : ITagRepository
    {
        private readonly DAMDbContext _context;

        public TagRepository(DAMDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Tag>> GetTagsAsync()
        {
            return await _context.Tags.ToListAsync();
        }

        public async Task<Tag> AddTagAsync(Tag tag)
        {
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
            return tag;
        }
        
        public async Task ClearTagsAsync()
        {
            _context.Tags.RemoveRange(_context.Tags);
            await _context.SaveChangesAsync();
        }

        public async Task ReplaceTagsAsync(IEnumerable<CreateTagDto> newTags)
        {

             var newNames = newTags
                .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                .Select(t => t.Name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            
            var newNamesLower = newNames.Select(name => name.ToLower()).ToHashSet();

            var currTags = await GetTagsAsync();

            
            // dict: map lowercase tag names to Tag entities
            var currTagsDict = currTags.ToDictionary(t => t.Name.Trim().ToLower(), t => t);

            // find the tags to remove (exist in db but not in the request)
            var tagsToRemove = currTags.Where(t => !newNamesLower.Contains(t.Name.Trim().ToLower())).ToList();
            
            foreach (var tag in tagsToRemove) 
            {
                // remove association in projectTags
                var projectTags = _context.ProjectTags.Where(pt => pt.TagID == tag.TagID);
                _context.ProjectTags.RemoveRange(projectTags);

                // remove association in assetTags
                var assetTags = _context.AssetTags.Where(at => at.TagID == tag.TagID);
                _context.AssetTags.RemoveRange(assetTags);

                // remove the tag
                _context.Tags.Remove(tag);
            }

            // which tags we need to add based on request
            var tagsToAdd = newNames.Where(name => !currTagsDict.ContainsKey(name.ToLower())).ToList();

            foreach (var name in tagsToAdd) {
                var newTag = new Tag { Name = name };
                _context.Tags.Add(newTag);
            }

            await _context.SaveChangesAsync();
        }
    }
}
