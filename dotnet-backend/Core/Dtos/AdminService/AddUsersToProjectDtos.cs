namespace Core.Dtos
{
    public class AddUsersToProjectReq
    {
        public List<int> addAsAdmin { get; set; }
        public List<int> addAsRegular { get; set; }
    }

    public class AddUsersToProjectRes
    {
        public int projectID { get; set; }
        public HashSet<int> newAdmins { get; set; }
        public HashSet<int> newRegularUsers { get; set; }
        public HashSet<int> failedToAddAsAdmin { get; set; }
        public HashSet<int> failedToAddAsRegular { get; set; }
    }
}
