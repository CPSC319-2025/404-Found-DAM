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
        public Task<int> ProcessUploadAsync(IFormFile file, UploadAssetsReq request)
        {
            
            return _paletteRepository.UploadAssets(file, request);
        }
    }
}
