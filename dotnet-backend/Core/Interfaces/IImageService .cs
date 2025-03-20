using Core.Dtos;

namespace Core.Interfaces
{
    public interface IImageService 
    {
        void rotate90();
        void toWebpImageSharp();
        byte[] toWebpNetVips(byte[] decompressedBuffer);
        void pHashCompare();
    }
}
