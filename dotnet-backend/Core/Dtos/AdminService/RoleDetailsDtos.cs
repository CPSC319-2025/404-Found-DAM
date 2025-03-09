namespace Core.Dtos
{
    public class RoleDetailsRes
    {
        public int userID { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public HashSet<string> roles  { get; set; }
    }  
}