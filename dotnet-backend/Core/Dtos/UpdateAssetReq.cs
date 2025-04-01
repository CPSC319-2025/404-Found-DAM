using System;

namespace Core.Dtos
{
    public class UpdateAssetReq
    {
        public string BlobId { get; set; }
        public string AssetMimeType { get; set; }
        public int UserId { get; set; }
    }
} 