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

namespace MockedData
{
    public static class MockedDataSeeding 
    {
        public static async Task Seed(IServiceScope scope) 
        {
            string filePath;
            string jsonString;
            var _contextFactory = scope.ServiceProvider.GetService<IDbContextFactory<DAMDbContext>>();

            // Add users
            // filePath = @"..\Core\MockedSeed\mockedUsers.json";
            // jsonString = File.ReadAllText(filePath);
            // List<User>? users = JsonSerializer.Deserialize<List<User>>(jsonString);

            // if (users != null && _contextFactory != null) 
            // {
            //     using DAMDbContext _context = _contextFactory.CreateDbContext();
            //     await _context.Users.AddRangeAsync(users);
            //     await _context.SaveChangesAsync();
            // }

            // Add projects (no users yet!)
            var adminService = scope.ServiceProvider.GetService<IAdminService>();
            if (adminService != null) 
            {
                filePath = @"..\Core\MockedSeed\mockedProjects.json";
                jsonString = File.ReadAllText(filePath);
                List<CreateProjectsReq>? req = JsonSerializer.Deserialize<List<CreateProjectsReq>>(jsonString);
                if (req != null) {
                    // TODO: replace mocked userID with authenticated userID
                    int userID = 1;
                    await adminService.CreateProjects(req, userID);
                }
            }
        }
    }
}