namespace Core.Dtos
{
    public class RegisterDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public bool IsSuperAdmin { get; set; }
    }
}