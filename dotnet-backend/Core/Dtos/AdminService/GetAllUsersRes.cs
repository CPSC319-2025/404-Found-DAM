namespace Core.Dtos
{
    public class GetAllUsersRes
    {
        public int UserCount { get; set; }
        
        public required List<UserDto> Users { get; set; }
    }
}