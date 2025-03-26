using Core.Entities;

namespace Core.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task CreateUserAsync(User user);
    }
}