namespace Core.Services.Utils
{
    public static class FileCompressionHelper 
    {  
        public static byte[] Compress(byte[] data)
        {
            try
            {
                // No compression, just return the original data
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Data handling error: {ex.Message}");
                throw new Exception($"Failed to process data: {ex.Message}", ex);
            }
        }

        // A method to decompress byte array (now just returns the data as-is)
        public static byte[] Decompress(byte[] compressedData)
        {
            try
            {
                // No decompression, just return the original data
                return compressedData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Data handling error: {ex.Message}");
                throw new Exception($"Failed to process data: {ex.Message}", ex);
            }
        }
    }
}