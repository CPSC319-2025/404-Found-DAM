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
            var _contextFactory = scope.ServiceProvider.GetService<IDbContextFactory<DAMDbContext>>();

            // Add users
            string filePath = @"..\Core\MockedSeed\mockedUsers.json";
            string jsonString = File.ReadAllText(filePath);
            List<User>? users = JsonSerializer.Deserialize<List<User>>(jsonString);

            if (users != null && _contextFactory != null) 
            {
                using DAMDbContext _context = _contextFactory.CreateDbContext();
                await _context.Users.AddRangeAsync(users);
                await _context.SaveChangesAsync();
            }


 

    
            // var adminService = scope.ServiceProvider.GetService<IAdminService>();
            // if (adminService != null) 
            // {
            //     CreateProjectsReq p = new CreateProjectsReq
            //     { 
            //         defaultMetadata = new DefaultMetadata
            //         {
            //             projectName = "seeding test",
            //             location = "Van",
            //             description = "A test",
            //             active = true
            //         },
            //         tags = new List<string> {"t1", "t2"}
            //     };

            //     List<CreateProjectsReq> req = new List<CreateProjectsReq> { p };
            //     await adminService.CreateProjects(req);
            // }
        }
    }
}