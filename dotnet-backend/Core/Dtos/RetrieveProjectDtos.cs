namespace Core.Dtos
{
    public class RetrieveProjectRes
    {
        public string projectId { get; set; }
        public string projectName { get; set; }
        public bool archived { get; set; }
        public string? archivedAt { get; set; }
        public string admin { get; set; }
    }
}