namespace Core.Entities.Dtos
{
    public class ExportProjectReq
    {
        public required string originalName { get; set; }
        public required string assignedName { get; set; }
        public required string projectId { get; set; }
        public string? userId { get; set; }
        public required string timeStamp { get; set; }
    }
}