using System.Text;

namespace DirectXEngine
{
    internal class StringProperty : SpecialProperty
    {
        public string Data { get; private set; }
        protected override int DataSizeInBytes => Data.Length;

        public static StringProperty Parse(byte[] data, int startIndex, uint length) => new StringProperty
        {
            Data = Encoding.ASCII.GetString(data, startIndex, (int)length)
        };
    }
}
