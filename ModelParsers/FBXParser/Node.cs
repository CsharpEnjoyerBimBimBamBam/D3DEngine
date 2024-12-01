using System;
using System.Text;

namespace DirectXEngine.FBXModelParser
{
    internal class Node
    {
        public uint EndOffset { get; private set; }
        public uint PropertiesCount { get; private set; }
        public uint PropertiesListLength { get; private set; }
        public byte NameLength { get; private set; }
        public string Name { get; private set; }
        public bool IsLast { get; private set; }
        public Property Property { get; private set; }
        private const int _Uint32Size = sizeof(uint);
        private const int _Uint8Size = sizeof(byte);

        public static Node Parse(byte[] data, int startNodeIndex)
        {
            Node node = new Node();
                
            int currentIndex = startNodeIndex;

            node.EndOffset = BitConverter.ToUInt32(data, currentIndex);
            currentIndex += _Uint32Size;

            node.PropertiesCount = BitConverter.ToUInt32(data, currentIndex);
            currentIndex += _Uint32Size;

            node.PropertiesListLength = BitConverter.ToUInt32(data, currentIndex);
            currentIndex += _Uint32Size;

            node.NameLength = data[currentIndex];
            currentIndex += _Uint8Size;

            byte nameLength = node.NameLength;
            node.Name = Encoding.ASCII.GetString(data, currentIndex, nameLength);
            currentIndex += nameLength;

            node.IsLast = node.EndOffset >= data.Length;
                
            if (node.PropertiesCount == 0)
                return node;

            node.Property = Property.Parse(data, currentIndex);

            return node;
        }
    }
}
