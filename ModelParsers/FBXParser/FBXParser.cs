using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace DirectXEngine.FBXModelParser
{
    public partial class FBXParser : ModelParser
    {
        public override bool CheckSignature(byte[] data)
        {
            const int signatureLength = 20;
            string signature = Encoding.ASCII.GetString(data, 0, signatureLength);
            return signature == _Siganture;
        }

        public override Mesh Parse(byte[] data)
        {
            const int startIndex = 27;
            int currentIndex = startIndex;

            Node node;
            bool isLast = false;
            while (!isLast)
            {
                node = Node.Parse(data, currentIndex);
                currentIndex = (int)node.EndOffset;
                isLast = node.IsLast;
                //MessageBox.Show(node.Name.ToString());
                //MessageBox.Show(node.NameLength.ToString());
            }

            
            throw new NotImplementedException();
        }

        private const string _Siganture = "Kaydara FBX Binary  ";

        internal delegate double Converter(byte[] data, int startIndex);
    }
}
