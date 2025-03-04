
using Core.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using ZstdNet;
using Microsoft.AspNetCore.Http;
using Core.Dtos;

namespace Infrastructure.DataAccess {
    public class PaletteRepository : IPaletteRepository {

        private readonly DAMDbContext _context;
        private readonly Decompressor decompressor;

        public PaletteRepository(DAMDbContext context) {
            _context = context;
            decompressor = new Decompressor();
        }
        public Task<List<Asset>> GetAssetsFromPalette() {
            return _context.Assets.ToListAsync();
        }
        public async Task<bool> UploadAssets(IFormFile file, UploadAssetsReq request)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            // Read the IFormFile into a byte array
            byte[] compressedData;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                compressedData = ms.ToArray();
            }

            // Decompress the data using Zstd (adjust based on your chosen library)
            byte[] decompressedData;
            using (var decompressor = new Decompressor())
            {
                decompressedData = decompressor.Unwrap(compressedData);
            }
            
            // Create an Asset instance with the decompressed data
            var asset = new Asset
            {
                Data = decompressedData,
                FileName = file.FileName,
                ProjectId = request.ProjectId,
                // Set any additional properties as needed
                UploadedAt = DateTime.UtcNow
            };

            // Add the asset to the database context and save changes
            await _context.Assets.AddAsync(asset);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}