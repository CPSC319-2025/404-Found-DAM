using Core.Dtos;

namespace Core.Interfaces
{
    public interface IUserService
    {
       Task<GetUserRes> GetUser(int userID);
    }
}