namespace Core.Dtos
{
    public class AddAssetsToProjectReq
    {
        public string projectId { get; set; }
        public List<string> ImageIds { get; set; }
    }
}