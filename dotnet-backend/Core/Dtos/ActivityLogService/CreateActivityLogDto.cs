namespace Core.Dtos
public class CreateActivityLogDto
{
    public int UserID { get; set; }
    public string ChangeType { get; set; }
    public string Description { get; set; }
    public int ProjectID { get; set; }
    public string AssetID { get; set; }

    public bool isAdminAction { get; set; }
}