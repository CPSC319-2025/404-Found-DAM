using Core.Services;
using Core.Interfaces;
using System.Threading.Tasks;
using Core.Dtos;
using Core.Entities;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace MockedData
{
    public static class MockedDataSeeding 
    {
        public static async Task Seed(IServiceScope scope) 
        {
            var _contextFactory = scope.ServiceProvider.GetService<IDbContextFactory<DAMDbContext>>();

            // Add users
            string mockedUsersfilePath = Path.Combine("..", "Core", "MockedSeed", "mockedUsers.json");
            string mockedUsersPathJsonString = File.ReadAllText(mockedUsersfilePath);
            List<User>? users = JsonSerializer.Deserialize<List<User>>(mockedUsersPathJsonString);

            if (users != null && _contextFactory != null) 
            {
                using DAMDbContext _context = _contextFactory.CreateDbContext();
                var passwordHasher = new PasswordHasher<User>();

                foreach (var user in users)
                {
                    if (!string.IsNullOrEmpty(user.PasswordHash))
                    {
                        user.PasswordHash = passwordHasher.HashPassword(user, user.PasswordHash);
                    }
                }

                await _context.Users.AddRangeAsync(users);
                await _context.SaveChangesAsync();
            }

            // Add projects (no users yet!)
            var adminService = scope.ServiceProvider.GetService<IAdminService>();
            if (adminService != null) 
            {
                string mockedProjectsfilePath = Path.Combine("..", "Core", "MockedSeed", "mockedProjects.json");
                string mockedProjectsPathJsonString = File.ReadAllText(mockedProjectsfilePath);
                List<CreateProjectsReq>? req = JsonSerializer.Deserialize<List<CreateProjectsReq>>(mockedProjectsPathJsonString);
                if (req != null) {
                    // TODO: replace mocked userID with authenticated userID
                    int userID = 1;
                    await adminService.CreateProjects(req, userID);
                }
            }
        }
    }
}