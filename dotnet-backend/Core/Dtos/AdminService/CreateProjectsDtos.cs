namespace Core.Dtos
{
    public class CreateProjectsReq
    {
        public required DefaultMetadata defaultMetadata { get; set; }
        public List<string>? tags { get; set; }

        public List<int>? admins { get; set; }
        public List<int>? users { get; set; }
    }

    public class DefaultMetadata
    {
        public required string projectName { get; set; }
        public string? location { get; set; }
        public string? description { get; set; }
        public required bool active { get; set; } = true;
    }

    public class CreateProjectsRes
    {
        public required int createdProjectID { get; set; }
    }
}