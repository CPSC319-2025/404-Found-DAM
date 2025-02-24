namespace Core.Entities.Dtos
{
    public class CreateProjectReq
    {
        public required Metadata metadataVal { get; set; }

        public class Metadata
        {
            public required string ProjectName { get; set; }
            public string? Location { get; set; }
            public List<string>? Tags { get; set; }
        }
    }
}