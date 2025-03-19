using Core.Entities;

namespace Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
    }
}
using Core.Entities;
using Core.Dtos;

namespace Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserById(int userID);
    }
}