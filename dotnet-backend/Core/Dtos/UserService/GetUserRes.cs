namespace Core.Dtos
{
    public class GetUserRes
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }

        public required List<UserRoleDto> Roles { get; set; }

    }

    public class UserRoleDto 
    {
        public required int ProjectID { get; set; }
        public required string Role { get; set; }
    }
}