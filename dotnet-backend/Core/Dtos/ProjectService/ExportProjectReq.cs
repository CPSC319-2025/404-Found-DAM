namespace Core.Dtos
{
    public class ExportProjectReq
    {
        public required string originalName { get; set; }
        public required string assignedName { get; set; }
        public required int projectID { get; set; }
        public int? userID { get; set; }
        public required string timeStamp { get; set; }
    }
}