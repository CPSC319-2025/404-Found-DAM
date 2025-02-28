
using Infrastructure.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess {
    public class PaletteRepository : IPaletteRepository {

        private readonly DAMDbContext _context;

        public PaletteRepository(DAMDbContext context) {
            _context = context;
        }
        public Task<List<Asset>> GetAssetsFromPalette() {
            return _context.Assets.ToListAsync();
        }

        public Task<bool> AddAssetsToPalette(List<Asset> assets) {
            throw new NotImplementedException();
        }

        public Task<bool> UploadAssets(List<Asset> assets) {
            throw new NotImplementedException();
        }
    }
}