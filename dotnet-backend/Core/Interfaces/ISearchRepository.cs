using Core.Entities;
using System;

namespace Core.Interfaces
{
    public interface ISearchRepository
    {
        Task<List<Project>> SearchProjectsAsync(string query);
        Task<List<Asset>> SearchAssetsAsync(string query);
    }
}