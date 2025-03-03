using System;
using System.IO;
using System.IO.Compression;

namespace Core.Services.Utils
{
    public static class ProjectServiceHelpers 
    {
        public static byte[] CompressByteArray(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var compressor  = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                compressor.Write(data, 0, data.Length);
                return compressedStream.ToArray();
            }
        }
    }
}