using Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using CoenM.ImageHash.HashAlgorithms;
using CoenM.ImageHash;
using NetVips;

namespace Core.Services
{
    public class ImageService : IImageService
    {
        public void rotate90() 
        {
            // var image = NetVips.Image.NewFromFile("treeRot90.jpg");
            // image = image.Rot90();
        }

        // Consider making toWebpNetVips async
        public byte[] toWebpNetVips(byte[] decompressedBuffer, bool lossless)
        {
            // var image = NetVips.Image.NewFromFile("SamplePNGImage_20mbmb.png");            
            try
            {
                using (var image = NetVips.Image.NewFromBuffer(decompressedBuffer))
                {
                    MemoryStream webpLossyStream = new MemoryStream();                    
                    byte[] webpLossyBuffer = image.WebpsaveBuffer(null, lossless); // WebpsaveBuffer(int? qFactor, bool lossless)
                    return webpLossyBuffer;
                } 
            }
            catch (VipsException)
            {
                throw;
            }
        }

        
        public void toWebpImageSharp()
        {
            // using var img = SixLabors.ImageSharp.Image.Load(@"treeRot90.jpg");
            // WebpEncoder encoder = new ()
            // {
            //     FileFormat = WebpFileFormatType.Lossless
            // };
            // img.SaveAsWebp($@"treeRot90.webp", encoder);   
        }

        public void pHashCompare() 
        {
            // var hashAlgorithm = new AverageHash();
            var hashAlgorithm = new DifferenceHash();
            // var hashAlgorithm = new PerceptualHash();
            string filename1 = "SamplePNGImage_20mbmb.png";
            string filename2 = "SamplePNGImage_20mbmb.webp";
            using var imageStream1 = File.OpenRead(filename1);
            using var imageStream2 = File.OpenRead(filename2);
            ulong hash1 = hashAlgorithm.Hash(imageStream1);
            ulong hash2 = hashAlgorithm.Hash(imageStream2);
            double percentageImageSimilarity = CompareHash.Similarity(hash1, hash2);
            // Console.WriteLine($"hash1: ${hash1}");
            // Console.WriteLine($"hash2: ${hash2}");
            // Console.WriteLine($"percentageImageSimilarity: ${percentageImageSimilarity}");
        }
    }
}


