using Core.Dtos;

namespace Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDtos?> AuthenticateAsync(string email, string password);
    }
}
