using Core.Interfaces;
using System.Linq;
using Infrastructure.Exceptions;

namespace Core.Services
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _tagRepository;

        public TagService(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<IEnumerable<string>> GetTagNamesAsync()
        {
            try {
                var tags = await _tagRepository.GetTagsAsync();
                return tags.Select(t => t.Name);
            } catch (DataNotFoundException) {
                throw;
            } catch (Exception) {
                throw;
            }
        }
    }
}
