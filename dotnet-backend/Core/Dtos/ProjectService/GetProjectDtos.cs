namespace Core.Dtos
{
    public class GetProjectRes
    {
        public int projectID { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string location { get; set; }
        public bool active { get; set; }
        public DateTime? archivedAt { get; set; }
        public List<UserCustomInfo> admins { get; set; } 
        public List<UserCustomInfo> regularUsers { get; set; } 
        public List<TagCustomInfo> tags { get; set; }
        public List<ProjectMetadataCustomInfo> metadataFields { get; set; }
    }
}