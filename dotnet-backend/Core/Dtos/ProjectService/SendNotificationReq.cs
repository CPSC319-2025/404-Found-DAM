namespace Core.Dtos
{
    public class SendNotificationReq
    {
        public required int userID { get; set; }
        public required string changeType { get; set; }
        public string? description { get; set; }
        public required string status { get; set; }
    }
}