namespace Core.Dtos
{
    public class CustomMetadataDto
    {
        public int? FieldID { get; set; }
        public string FieldName { get; set; }
        public string FieldType { get; set; }
        public bool IsEnabled { get; set; }
        public string FieldValue { get; set; }
    }
}