using Core.Interfaces;
using Core.Dtos;
using Microsoft.AspNetCore.Http;


namespace Core.Services
{
    public class PaletteService : IPaletteService
    {
        private readonly IPaletteRepository _paletteRepository;
        
        public PaletteService(IPaletteRepository paletteRepository)
        {
            _paletteRepository = paletteRepository;
        }

        public async Task<bool> ProcessUploadAsync(IFormFile file, UploadAssetsReq request)
        {
            try {
                return await _paletteRepository.UploadAssets(file, request);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error uploading assets: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool[]> ProcessUploadsAsync(IList<IFormFile> files, UploadAssetsReq request)
        {
            var tasks = files.Select(file => ProcessUploadAsync(file, request)).ToArray();
            return await Task.WhenAll(tasks);
        }
    }
}
