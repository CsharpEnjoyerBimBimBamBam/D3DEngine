using System.Net.Configuration;
using System.Windows.Forms;
using System;
using System.IO;

namespace DirectXEngine.FBXModelParser
{
    public abstract class ModelParser
    {
        public static ModelParser FBXParser { get; } = new FBXParser();
        private static ModelParser[] _Parsers = new ModelParser[]
        {
            FBXParser
        };

        public abstract bool CheckSignature(byte[] data);

        public abstract Mesh Parse(byte[] data);
        
        public static ModelParser GetBySignature(byte[] data)
        {
            foreach (ModelParser parser in _Parsers)
            {
                if (parser.CheckSignature(data))
                    return parser;
            }

            throw new Exception("Can not find suitable parser for data signature");
        }

        public static Mesh ParseModel(byte[] data) => GetBySignature(data).Parse(data);

        public static Mesh ParseModel(string path) => ParseModel(File.ReadAllBytes(path));
    }
}
