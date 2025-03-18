namespace Core.Dtos
{
    public class GetProjectRes
    {
        public int projectID { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string location { get; set; }
        public bool archived { get; set; }
        public DateTime? archivedAt { get; set; }
        public List<string> adminNames { get; set; }
        public List<string> regularUserNames { get; set; }
        public List<string> tags { get; set; }
    }
}