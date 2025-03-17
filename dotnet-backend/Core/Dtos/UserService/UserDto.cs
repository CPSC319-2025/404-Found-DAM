namespace Core.Dtos
{
    public class UserDto
    {
        public int UserID { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
    }
}