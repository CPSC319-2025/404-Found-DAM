using Core.Entities;

namespace Core.Interfaces
{
    public interface ITagRepository
    {
        Task<IEnumerable<Tag>> GetTagsAsync();
        Task<Tag> AddTagAsync(Tag tag);
    }
}