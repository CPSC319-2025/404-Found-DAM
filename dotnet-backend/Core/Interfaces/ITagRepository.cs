using Core.Dtos;
using Core.Entities;

namespace Core.Interfaces
{
    public interface ITagRepository
    {
        Task<IEnumerable<Tag>> GetTagsAsync();
        Task ClearTagsAsync();

        Task ReplaceTagsAsync(IEnumerable<CreateTagDto> newTags);
        Task<Tag> AddTagAsync(Tag tag);
    }
}