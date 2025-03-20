using Core.Entities;
using Core.Dtos;

namespace Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserById(int userID);
    }
}