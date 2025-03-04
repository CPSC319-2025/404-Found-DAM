using Core.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Core.Dtos;
using ZstdSharp;

namespace Infrastructure.DataAccess {
    public class PaletteRepository : IPaletteRepository {

        private readonly IDbContextFactory<DAMDbContext> _contextFactory;
        private readonly bool _useZstd;

        public PaletteRepository(IDbContextFactory<DAMDbContext> contextFactory) 
        {
            _contextFactory = contextFactory;
        }
        
        private byte[] DecompressData(byte[] compressedData)
        {
            try
            {
                Decompressor _decompressor = new Decompressor();
                return _decompressor.Unwrap(compressedData).ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decompression error: {ex.Message}");
                throw new Exception($"Failed to decompress data: {ex.Message}", ex);
            }
        }
        
        public Task<List<Asset>> GetAssetsFromPalette() {
            using var _context = _contextFactory.CreateDbContext();
            return _context.Assets.ToListAsync();
        }
        
        public async Task<bool> UploadAssets(IFormFile file, UploadAssetsReq request)
        {
            using var _context = _contextFactory.CreateDbContext();
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            // Read the IFormFile into a byte array
            byte[] compressedData;
            try {
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    compressedData = ms.ToArray();
                }

                // Decompress the data using ZstdSharp
                byte[] decompressedData = DecompressData(compressedData);
                
                // Create storage directory if it doesn't exist
                string storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
                if (!Directory.Exists(storageDirectory))
                {
                    Directory.CreateDirectory(storageDirectory);
                }

                // Generate a unique filename
                string uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                string filePath = Path.Combine(storageDirectory, uniqueFileName);

                // Save the decompressed data to a file
                await File.WriteAllBytesAsync(filePath, decompressedData);
                
                // Create an Asset instance with the file path
                var asset = new Asset
                {
                    FileName = file.FileName,
                    MimeType = file.ContentType,
                    ProjectID = null,
                    UserID = request.UserId,
                };

            // Add the asset to the database context and save changes
            
                await _context.Assets.AddAsync(asset);
                int num = await _context.SaveChangesAsync();
            } catch (Exception ex) {
                Console.WriteLine($"Error saving asset to database: {ex.Message}");
                return false;
            }

            return true;
        }
    }
}