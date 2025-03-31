using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Interfaces;
using Core.Dtos;
using Core.Entities;
using ClosedXML.Excel;
using Core.Services.Utils;
using Infrastructure.Exceptions;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<GetUserRes> GetUser(int userID)
        {
            var user = await _repository.GetUserById(userID);
            if (user == null)
            {
                // throw exception if user not found
                throw new DataNotFoundException($"User with id {userID} not found.");
            }
            var roles = user.ProjectMemberships.Select(pm => new UserRoleDto {
                ProjectID = pm.ProjectID,
                Role = pm.UserRole.ToString().ToLower() //converts to regular or admin
            }).ToList();

            return new GetUserRes
            {
                Id = user.UserID,
                Name = user.Name,
                Email = user.Email,
                Roles = roles
            };
        }

    }
}
