using Core.Dtos;

namespace Core.Interfaces
{
    public interface IImageService 
    {
        void rotate90();
        void toWebpImageSharp();
        byte[] toWebpNetVips(byte[] decompressedBuffer, bool lossless);
        void pHashCompare();
    }
}
