namespace Core.Dtos
{
    public class GetAssetsRes
    {
        public List<string> BlobUris { get; set; } = new List<string>();
        public List<string> FileNames { get; set; } = new List<string>();
    }

}
