namespace Core.Dtos
{
    public class DeleteUsersFromProjectReq
    {
        public List<int> removeFromAdmins { get; set; }
        public List<int> removeFromRegulars { get; set; }
    }

    public class DeleteUsersFromProjectRes
    {
        public int projectID { get; set; }
        public HashSet<int> removedAdmins { get; set; }
        public HashSet<int> removedRegularUsers { get; set; }
        public HashSet<int> failedToRemoveFromAdmins { get; set; }
        public HashSet<int> failedToRemoveFromRegulars { get; set; }
    }
}
