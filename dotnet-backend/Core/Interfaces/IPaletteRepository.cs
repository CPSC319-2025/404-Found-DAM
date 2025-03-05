using Core.Dtos;
using Core.Entities;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces  {
    public interface IPaletteRepository {
        public Task<List<Asset>> GetAssetsFromPalette();

        public Task<bool> UploadAssets(IFormFile file, UploadAssetsReq request);

    }
}