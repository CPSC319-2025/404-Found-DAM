using Core.Entities;

namespace Core.Interfaces
{
    public interface ITagRepository
    {
        Task<IEnumerable<Tag>> GetTagsAsync();
        Task ClearTagsAsync();
        Task<Tag> AddTagAsync(Tag tag);
    }
}