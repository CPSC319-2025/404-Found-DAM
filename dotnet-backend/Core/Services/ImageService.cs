using Core.Interfaces;
// using NetVips;
// using SixLabors.ImageSharp;
// using SixLabors.ImageSharp.Formats.Webp;

namespace Core.Services
{
    public class ImageService : IImageService
    {
        public void rotate90() 
        {
            // var image = NetVips.Image.NewFromFile("treeRot90.jpg");
            // image = image.Rot90();
            // image.Webpsave("treeRot90webp.webp", null, false);
        }

        public void toWebp()
        {
            // using var img = SixLabors.ImageSharp.Image.Load(@"treeRot90.jpg");
            // WebpEncoder encoder = new ()
            // {
            //     FileFormat = WebpFileFormatType.Lossless
            // };
            // img.SaveAsWebp($@"treeRot90.webp", encoder);   
        }
    }
}


