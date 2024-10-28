using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace DirectXEngine
{
    internal class RendererData
    {
        public Renderer Renderer;
        public RendererGraphicsSettings Settings;
        public ShaderConstantData ConstantData;
    }
}
