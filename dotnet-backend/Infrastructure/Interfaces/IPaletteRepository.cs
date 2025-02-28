using Core.Entities;

namespace Infrastructure.Interfaces  {
    public interface IPaletteRepository {
        public Task<List<Asset>> GetAssetsFromPalette();

        public Task<bool> AddAssetsToPalette(List<Asset> assets);

        public Task<bool> UploadAssets(List<Asset> assets);

    }
}