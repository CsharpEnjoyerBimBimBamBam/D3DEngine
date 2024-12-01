using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DirectXEngine
{
    internal abstract class Property
    {
        public char Type { get; private set; }
        public int FullSizeInBytes { get; private set; }
        public abstract int PropertySizeInBytes { get; }
        private static Dictionary<char, PropertyType> _PrimitiveTypes { get; } = new Dictionary<char, PropertyType>
        {
            { 'Y', new PropertyType { Converter = (data, startIndex) => BitConverter.ToInt16(data, startIndex), Size = sizeof(short) } },
            { 'C', new PropertyType { Converter = (data, startIndex) => data[startIndex], Size = sizeof(byte) } },
            { 'I', new PropertyType { Converter = (data, startIndex) => BitConverter.ToInt32(data, startIndex), Size = sizeof(int) } },
            { 'F', new PropertyType { Converter = (data, startIndex) => BitConverter.ToSingle(data, startIndex), Size = sizeof(float) } },
            { 'D', new PropertyType { Converter = BitConverter.ToDouble, Size = sizeof(double) } },
            { 'L', new PropertyType { Converter = (data, startIndex) => BitConverter.ToInt64(data, startIndex), Size = sizeof(double) } },
        };
        private static Dictionary<char, PropertyType> _ArrayTypes { get; } = new Dictionary<char, PropertyType>
        {
            { 'f', _PrimitiveTypes['F'] },
            { 'd', _PrimitiveTypes['D'] },
            { 'l', _PrimitiveTypes['L'] },
            { 'i', _PrimitiveTypes['I'] },
            { 'b', _PrimitiveTypes['C'] },
        };

        public static Property Parse(byte[] data, int startIndex)
        {
            int currentIndex = startIndex;
            
            char type = Convert.ToChar(data[currentIndex]);
            currentIndex++;
            MessageBox.Show(type.ToString());
            Property property = default;
            return property;
            if (ArrayProperty.IsArrayType(type))
                property = ArrayProperty.Parse(data, currentIndex, _ArrayTypes[type]);
            else if (PrimitiveProperty.IsPrimitiveProperty(type))
                property = PrimitiveProperty.Parse(data, currentIndex, _PrimitiveTypes[type]);
            else if (SpecialProperty.IsSpecialProperty(type))
                property = SpecialProperty.Parse(data, currentIndex, type);

            property.FullSizeInBytes = (3 * sizeof(uint)) + property.PropertySizeInBytes;

            return property;
        }
    }
}
