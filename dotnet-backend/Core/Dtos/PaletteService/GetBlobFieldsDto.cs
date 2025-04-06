using System.Collections.Generic;

namespace Core.Dtos
{
    public class BlobFieldDto
    {
        public int FieldId { get; set; }
        public string FieldValue { get; set; }
        public string FieldName { get; set; }
        public string FieldType { get; set; }
    }

    public class GetBlobFieldsRes
    {
        public string BlobId { get; set; }
        public List<BlobFieldDto> Fields { get; set; } = new List<BlobFieldDto>();
    }
} 