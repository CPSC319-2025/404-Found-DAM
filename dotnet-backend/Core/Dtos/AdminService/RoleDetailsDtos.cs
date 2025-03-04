namespace Core.Dtos
{
    public class RoleDetailsRes
    {
        public int userId { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public List<string> roles  { get; set; }
        public DateTime lastModified { get; set; }
    }  
}