using System;
using System.Linq;

namespace DirectXEngine
{
    internal class ArrayProperty : Property
    {
        public uint Length;
        public uint Encoding;
        public uint CompressedLength;
        public double[] Data;
        private static char[] _PossibleTypes { get; } = new char[]
        {
            'f', 'd', 'l', 'i', 'b',
        };
        public override int PropertySizeInBytes => _SizeInBytes;
        private int _SizeInBytes;
        private const int _Uint32Size = sizeof(uint);

        public static bool IsArrayType(char type) => _PossibleTypes.Contains(type);

        public static ArrayProperty Parse(byte[] data, int startIndex, PropertyType type)
        {
            Converter converter = type.Converter;
            int size = type.Size;

            ArrayProperty arrayProperty = new ArrayProperty();
            int currentIndex = startIndex;

            arrayProperty.Length = BitConverter.ToUInt32(data, currentIndex);
            currentIndex += _Uint32Size;

            arrayProperty.Encoding = BitConverter.ToUInt32(data, currentIndex);
            currentIndex += _Uint32Size;

            arrayProperty.CompressedLength = BitConverter.ToUInt32(data, currentIndex);
            currentIndex += _Uint32Size;

            int length = (int)arrayProperty.Length;
            arrayProperty.Data = new double[length];

            for (int i = 0; i < length; i++)
            {
                arrayProperty.Data[i] = converter.Invoke(data, currentIndex);
                currentIndex += size;
            }

            int arraySize = length * size;
            arrayProperty._SizeInBytes = arraySize;

            return arrayProperty;
        }
    }
}
