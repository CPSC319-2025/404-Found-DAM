using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Core.Interfaces;

namespace Infrastructure.DataAccess
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DAMDbContext _context;

        public AuthRepository(DAMDbContext context)
        {
            _context = context;
        }
        
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.ProjectMemberships) // Load related project memberships if needed
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        
        public async Task CreateUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
    }
}
