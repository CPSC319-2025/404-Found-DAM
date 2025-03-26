namespace Core.Dtos
{
    public class CreateActivityLogDto
    {
        public int userID { get; set; }
        public string changeType { get; set; }
        public string description { get; set; }
        public int projectID { get; set; }
        public string assetID { get; set; }

        public bool isAdminAction { get; set; }
    }
}