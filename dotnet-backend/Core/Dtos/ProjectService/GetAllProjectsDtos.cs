namespace Core.Dtos
{
    public class GetAllProjectsRes
    {
        public int projectCount { get; set; }
        public List<FullProjectInfo> fullProjectInfos { get; set; }
    }

    public class FullProjectInfo
    {
        public int projectID { get; set; }
        public string projectName { get; set; }
        public string location { get; set; }
        public string description { get; set; }
        public DateTime creationTime { get; set; }
        public bool active { get; set; }
        public DateTime? archivedAt { get; set; }
        public int assetCount { get; set; }
        public HashSet<UserCustomInfo> admins { get; set; }
        public HashSet<UserCustomInfo> regularUsers { get; set; }

        public FullProjectInfo()
        {
            admins = new HashSet<UserCustomInfo>();
            regularUsers = new HashSet<UserCustomInfo>();
        }
    }
}