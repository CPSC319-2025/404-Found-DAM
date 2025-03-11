namespace Core.Dtos
{
    public class ModifyRoleReq
    {
        public required string roleChangeTo { get; set; }
    }

    public class ModifyRoleRes
    {
        public int projectID { get; set; }
        public int userID { get; set; }
        public string updatedRole { get; set; }
        public DateTime updatedAt { get; set; }
    }
}