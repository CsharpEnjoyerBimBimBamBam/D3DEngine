namespace DirectXEngine
{
    internal class ManualDrawDescription
    {
        public ManualDrawDescription(Shader shader, byte[] constantBufferData)
        {
            Shader = shader;
            ConstantBufferData = constantBufferData;
        }

        public Shader Shader { get; }
        public byte[] ConstantBufferData { get; }
    }
}
