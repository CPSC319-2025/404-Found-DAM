using Core.Dtos;

namespace Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> AuthenticateAsync(string email, string password);
    }
}
