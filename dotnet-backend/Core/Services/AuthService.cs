using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Core.Interfaces;
using Core.Entities;
using Core.Dtos;
using Newtonsoft.Json;
using System.Linq;

namespace Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<User> _passwordHasher;
        
        public AuthService(IAuthRepository AuthRepository, IConfiguration configuration)
        {
            _userRepository = AuthRepository;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<User>();
        }
        
        public async Task<AuthResponseDto?> AuthenticateAsync(string email, string password)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                return null;
            
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
                return null;
            
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("isSuperAdmin", user.IsSuperAdmin.ToString()),
                new Claim("userId", user.UserID.ToString())
            };
            
            var membershipsForClaim = user.ProjectMemberships
                .Select(pm => new { pm.ProjectID, Role = pm.UserRole.ToString() })
                .ToList();
            string membershipsJson = JsonConvert.SerializeObject(membershipsForClaim);
            claims.Add(new Claim("projectMemberships", membershipsJson));
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            var expires = DateTime.UtcNow.AddHours(1);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                SigningCredentials = creds,
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            
            var membershipsDto = user.ProjectMemberships.Select(pm => new ProjectMembershipAuthDto
            {
                ProjectId = pm.ProjectID,
                Role = pm.UserRole.ToString().ToLower()
            }).ToList();
            
            var authResponse = new AuthResponseDto
            {
                Token = tokenString,
                Id = user.UserID,
                Email = user.Email,
                IsSuperAdmin = user.IsSuperAdmin,
                ProjectMemberships = membershipsDto
            };
            
            return authResponse;
        }

        
        public async Task<int?> RegisterAsync(string email, string password, string name, bool isSuperAdmin)
        {
            var existingUser = await _userRepository.GetUserByEmailAsync(email);
            if (existingUser != null)
                return null;

            var passwordHasher = new PasswordHasher<User>();
            var hashedPassword = passwordHasher.HashPassword(null, password);
            
            var newUser = new User
            {
                Email = email,
                Name = name,
                PasswordHash = hashedPassword,
                IsSuperAdmin = isSuperAdmin
            };
            
            await _userRepository.CreateUserAsync(newUser);

            return newUser.UserID;
        }
    }
}