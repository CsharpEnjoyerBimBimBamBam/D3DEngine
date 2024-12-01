using SharpDX.Direct2D1;
using System;
using System.Linq;
using System.Windows.Forms;

namespace DirectXEngine
{
    internal abstract class SpecialProperty : Property
    {
        public uint Length { get; private set; }
        public override int PropertySizeInBytes => sizeof(uint) + DataSizeInBytes;
        protected abstract int DataSizeInBytes { get; }
        private static char[] _PossibleTypes { get; } = new char[]
        {
            'R', 'S', //'r', 's'
        };
        private const int _Uint32Size = sizeof(uint);

        public static bool IsSpecialProperty(char type) => _PossibleTypes.Contains(type);

        public static SpecialProperty Parse(byte[] data, int startIndex, char type)
        {
            int currentIndex = startIndex;
            uint length = BitConverter.ToUInt32(data, currentIndex);
            currentIndex += _Uint32Size;

            type = char.ToLower(type);
            bool isStringPreperty = type == 's';

            //MessageBox.Show(isStringPreperty.ToString());
            //MessageBox.Show(length.ToString());

            SpecialProperty property;
            if (isStringPreperty)
                property = StringProperty.Parse(data, currentIndex, length);
            else
                property = RawBynaryDataProperty.Parse(data, currentIndex, length);

            property.Length = length;
            return property;
        }
    }
}
