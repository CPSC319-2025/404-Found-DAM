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
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        
        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }
        
        public async Task<AuthResponseDto?> AuthenticateAsync(string email, string password)
        {
            // Retrieve user by email
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                return null;
            
            // Validate password using PasswordHasher
            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
                return null;
            
            // Create claims for the token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("isSuperAdmin", user.IsSuperAdmin.ToString()),
                new Claim("userId", user.UserID.ToString())
            };
            
            // include project memberships as a claim
            var membershipsForClaim = user.ProjectMemberships
                .Select(pm => new { pm.ProjectID, Role = pm.UserRole.ToString() })
                .ToList();
            string membershipsJson = JsonConvert.SerializeObject(membershipsForClaim);
            claims.Add(new Claim("projectMemberships", membershipsJson));
            
            // Generate JWT token with a 3-hour expiry
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            var expires = DateTime.UtcNow.AddHours(3);
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
            
            // Map project memberships to DTOs
            var membershipsDto = user.ProjectMemberships.Select(pm => new ProjectMembershipDto
            {
                ProjectId = pm.ProjectID,
                Role = pm.UserRole.ToString().ToLower()
            }).ToList();
            
            // Build the authentication response DTO
            var authResponse = new AuthResponseDto
            {
                Token = tokenString,
                ExpiresIn = (int)(expires - DateTime.UtcNow).TotalSeconds,
                Id = user.UserID,
                Email = user.Email,
                IsSuperAdmin = user.IsSuperAdmin,
                ProjectMemberships = membershipsDto
            };
            
            return authResponse;
        }
    }
}
