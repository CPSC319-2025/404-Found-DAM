using Core.Interfaces;
using Core.Dtos;
using Microsoft.AspNetCore.Http;
using System.Reflection.Metadata;
using Microsoft.IdentityModel.Tokens;
using Infrastructure.Exceptions;
using Core.Entities;
using System.IO;
using ZstdSharp;

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

        public async Task ReplaceAllTagsAsync(IEnumerable<CreateTagDto> newTags)
        {
            await _tagRepository.ReplaceTagsAsync(newTags);
        }

    }
}
