namespace Core.Dtos
{
    public class AddMetadataReq
    {
        public required string fieldName { get; set; }
        public required string fieldType { get; set; }
    }

    public class AddMetadataRes
    {
        public int fieldID { get; set; }
        public string message { get; set; }
    }
}
