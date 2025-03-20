using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Core.Interfaces;
using Core.Dtos;
using Core.Entities;
using Infrastructure.Exceptions;
using System.Reflection.Metadata.Ecma335;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Infrastructure.DataAccess
{
    public class UserRepository : IUserRepository
    {
        private readonly DAMDbContext _context;

        public UserRepository(DAMDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserById(int userID)
        {
            var user = await _context.Users
                .Include(u => u.ProjectMemberships) // load project memberships
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == userID) ?? throw new DataNotFoundException($"User with id {userID} not found.");
            return user;
        }

    }
}