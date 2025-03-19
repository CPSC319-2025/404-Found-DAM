using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Core.Interfaces;

namespace Infrastructure.DataAccess
{
    public class UserRepository : IUserRepository
    {
        private readonly DAMDbContext _context;

        public UserRepository(DAMDbContext context)
        {
            _context = context;
        }
        
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.ProjectMemberships) 
                .FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
