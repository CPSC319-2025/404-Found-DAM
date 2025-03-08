namespace Core.Dtos
{
    public class DeletePaletteAssetReq
    {
        public required int UserId { get; set; }
        public required string Name { get; set; }
    }
}