using Core.Dtos;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces
{
    public interface ITagService
    {
        Task<IEnumerable<string>> GetTagNamesAsync();
        Task ReplaceAllTagsAsync(IEnumerable<CreateTagDto> newTags);
    }
}
