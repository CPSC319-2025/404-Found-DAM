namespace Core.Dtos
{
    public class SendStatusUpdateReq
    {
        public required int userID { get; set; }
        public required string changeType { get; set; }
        public required string status { get; set; }
        public string? message { get; set; }
    }
}