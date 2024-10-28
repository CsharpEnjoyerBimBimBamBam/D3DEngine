using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Windows.Forms;

namespace DirectXEngine
{
    public class NonIndexedNonInstancedDraw : RendererGraphicsSettings
    {
        public NonIndexedNonInstancedDraw(byte[] vertexBufferData, int vertexBufferStride) : base(vertexBufferData, vertexBufferStride)
        {

        }

        public override DrawMode DrawMode => DrawMode.Default;

        internal override void Draw(DeviceContext context)
        {
            context.InputAssembler.SetVertexBuffers(0, VertexBufferBinding);
            context.Draw(VerticesCount, 0);
        }
    }
}
