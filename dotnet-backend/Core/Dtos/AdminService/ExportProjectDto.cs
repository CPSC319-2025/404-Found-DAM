using Core.Entities;

namespace Core.Dtos
{
    public class ExportProjectDto
    {
        public required string fieldName { get; set;}
        public required ProjectMetadataField.FieldDataType fieldDataType { get; set;}
        public string? assetMetadataVal { get; set;}
    }
}