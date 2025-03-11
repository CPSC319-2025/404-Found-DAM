using Core.Dtos;

namespace Core.Interfaces
{
    public interface ISearchService 
    {
        Task<SearchResultDto> SearchAsync(string query);
    }
}
