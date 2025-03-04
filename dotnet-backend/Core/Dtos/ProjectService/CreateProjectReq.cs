namespace Core.Dtos
{
    public class CreateProjectReq
    {
        public required Metadata metadataVal { get; set; }

        public class Metadata
        {
            public required string projectName { get; set; }
            public string? location { get; set; }
            public List<string>? tags { get; set; }
        }
    }
}