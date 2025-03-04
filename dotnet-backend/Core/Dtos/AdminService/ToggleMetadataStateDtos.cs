namespace Core.Dtos
{
    public class ToggleMetadataStateReq
    {
        public required bool enabled { get; set; }
    }

    public class ToggleMetadataStateRes
    {
        public int fieldId { get; set; }
        public bool enabled { get; set; }
        public string message { get; set; }
    }  
}