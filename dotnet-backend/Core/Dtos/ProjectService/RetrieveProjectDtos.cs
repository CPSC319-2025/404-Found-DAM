namespace Core.Dtos
{
    public class RetrieveProjectRes
    {
        public int projectID { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string location { get; set; }
        public bool archived { get; set; }
        public string? archivedAt { get; set; }
        public string admin { get; set; }
        public List<string> tags { get; set; }
    }
}