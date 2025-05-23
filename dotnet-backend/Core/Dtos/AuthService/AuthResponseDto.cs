using System.Collections.Generic;

namespace Core.Dtos
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
        public List<ProjectMembershipAuthDto> ProjectMemberships { get; set; } = new List<ProjectMembershipAuthDto>();
    }
}