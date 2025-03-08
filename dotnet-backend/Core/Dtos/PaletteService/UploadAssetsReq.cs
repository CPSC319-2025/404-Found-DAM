namespace Core.Dtos.PaletteService
{
    public class UploadAssetsReq
    {
        public required string Name { get; set; }
        public required string Type { get; set; }
        public required int UserId { get; set; }

        public int ProjectID { get; set; }
    }
}
