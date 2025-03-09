namespace Core.Dtos
{
    public class ModifyRoleReq
    {
        public required bool userToAdmin { get; set; }
    }

    public class ModifyRoleRes
    {
        public DateTime updatedAt { get; set; }
    }
}