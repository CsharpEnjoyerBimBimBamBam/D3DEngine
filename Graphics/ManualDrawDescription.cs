using SharpDX.Direct3D11;

namespace DirectXEngine
{
    internal class ManualDrawDescription
    {
        public ManualDrawDescription(ShaderConstantData constantData, byte[] constantBufferData)
        {
            ConstantData = constantData;
            ConstantBufferData = constantBufferData;
        }

        public ShaderConstantData ConstantData { get; }
        public byte[] ConstantBufferData { get; }
    }
}
