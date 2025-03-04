using System;
using System.Linq;
using System.Collections.Generic;
using Core.Interfaces;
using Core.Dtos;
using DataModel;

namespace Infrastructure.DataAccess
{
    public class EFCoreAdminRepository : IAdminRepository
    {
        private MyDbContext _context;
        public EFCoreAdminRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<(bool, string)> ToggleMetadataCategoryActivationInDb(int projectID, int metadataFieldID, bool setEnabled)
        {
            //TODO
            // get project first
            // use project id to access metadata and update
            return (setEnabled, "DummyCategory");
        }

        public async Task<(User, List<string>)> GetRoleDetailsInDb(int userID)
        {
            //TODO
            // get User
            // get roles that this user plays
            User user = new User
            {
                UserID = 123,
                Name = "John",
                Email = "abc@yes.com",
                IsSuperAdmin = true,
                LastUpdated = DateTime.Now
            };
            List<string> roles = new List<string>();
            roles.Add("Admin");
            return (user, roles);
        }
    }
}
