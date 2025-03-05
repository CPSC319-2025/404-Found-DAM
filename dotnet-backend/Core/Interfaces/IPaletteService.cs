


using Core.Dtos;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces
{
    public interface IPaletteService
    {
        Task<bool> ProcessUploadAsync(IFormFile file, UploadAssetsReq request);
        Task<bool[]> ProcessUploadsAsync(IList<IFormFile> files, UploadAssetsReq request);
    }
}
