using ZstdSharp;

namespace Core.Services.Utils
{
    public static class FileCompressionHelper 
    {  
        public static byte[] Compress(byte[] data)
        {
            try
            {
                Compressor _compressor = new Compressor();
                return _compressor.Wrap(data).ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Compression error: {ex.Message}");
                throw new Exception($"Failed to compress data: {ex.Message}", ex);
            }
        }

        // A method to compress byte array
        public static byte[] Decompress(byte[] compressedData)
        {
            try
            {
                Decompressor _decompressor = new Decompressor();
                return _decompressor.Unwrap(compressedData).ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decompression error: {ex.Message}");
                throw new Exception($"Failed to decompress data: {ex.Message}", ex);
            }
        }
    }
}