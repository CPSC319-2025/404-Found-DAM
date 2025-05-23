using Core.Dtos;
using Core.Entities;

namespace Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> AuthenticateAsync(string email, string password);
        Task<int?> RegisterAsync(string email, string password, string name, bool isSuperAdmin);
    }
}