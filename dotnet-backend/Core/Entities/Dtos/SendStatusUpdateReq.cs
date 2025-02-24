namespace Core.Entities.Dtos
{
    public class SendStatusUpdateReq
    {
        public required string userId { get; set; }
        public required string changeType { get; set; }
        public required string status { get; set; }
        public string? message { get; set; }
    }
}