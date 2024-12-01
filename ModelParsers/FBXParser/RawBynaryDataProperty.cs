using System.Windows.Forms;

namespace DirectXEngine
{
    internal class RawBynaryDataProperty : SpecialProperty
    {
        public byte[] Data { get; private set; }
        protected override int DataSizeInBytes => Data.Length;

        public static RawBynaryDataProperty Parse(byte[] data, int startIndex, uint length)
        {
            RawBynaryDataProperty property = new RawBynaryDataProperty();
            property.Data = new byte[length];
            
            for (int i = 0; i < length; i++)
                property.Data[i] = data[startIndex + i];

            return property;
        }
    }
}
