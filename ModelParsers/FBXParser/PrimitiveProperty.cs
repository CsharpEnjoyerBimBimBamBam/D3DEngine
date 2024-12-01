using SharpDX.D3DCompiler;
using System.Linq;

namespace DirectXEngine
{
    internal class PrimitiveProperty : Property
    {
        public double Data { get; private set; }
        private static char[] _PossibleTypes { get; } = new char[]
        {
            'Y', 'C', 'I', 'F', 'D', 'L',
        };
        public override int PropertySizeInBytes => _PropertySizeInBytes;
        private int _PropertySizeInBytes;

        public static bool IsPrimitiveProperty(char type) => _PossibleTypes.Contains(type);

        public static PrimitiveProperty Parse(byte[] data, int startIndex, PropertyType type)
        {
            PrimitiveProperty property = new PrimitiveProperty();
            property._PropertySizeInBytes = type.Size;
            property.Data = type.Converter.Invoke(data, startIndex);
            return property;
        }
    }
}
