


using Core.Dtos;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces
{
    public interface IPaletteService
    {
        Task<int> ProcessUploadAsync(IFormFile file, UploadAssetsReq request);
    }
}
