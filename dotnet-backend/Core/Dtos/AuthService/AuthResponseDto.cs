using System.Collections.Generic;

namespace Core.Dtos
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
        public List<ProjectMembershipDto> ProjectMemberships { get; set; } = new List<ProjectMembershipDto>();
    }
}