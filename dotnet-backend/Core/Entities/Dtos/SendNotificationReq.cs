namespace Core.Entities.Dtos
{
    public class SendNotificationReq
    {
        public required string userId { get; set; }
        public required string changeType { get; set; }
        public string? description { get; set; }
        public required string status { get; set; }
    }
}