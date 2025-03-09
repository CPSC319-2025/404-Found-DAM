using Core.Services;
using Core.Interfaces;
using System.Threading.Tasks;
using Core.Dtos;


namespace MockedData
{
    public static class MockedDataSeeding 
    {
        public static async Task Seed(IServiceScope scope) 
        {
            var adminService = scope.ServiceProvider.GetService<IAdminService>();
            if (adminService != null) 
            {
                CreateProjectsReq p = new CreateProjectsReq
                { 
                    defaultMetadata = new DefaultMetadata
                    {
                        projectName = "seeding test",
                        location = "Van",
                        description = "A test",
                        active = true
                    },
                    tags = new List<string> {"t1", "t2"}
                };

                List<CreateProjectsReq> req = new List<CreateProjectsReq> { p };
                await adminService.CreateProjects(req);
            }
        }
    }
}