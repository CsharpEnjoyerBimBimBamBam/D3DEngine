using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace DirectXEngine
{
    public class IndexedDraw : RendererGraphicsSettings
    {
        public IndexedDraw(byte[] vertexBufferData, int vertexBufferStride, byte[] indexBufferData, Format format) 
            : base(vertexBufferData, vertexBufferStride)
        {
            ExceptionHelper.ThrowIfNull(indexBufferData);

            int stride = format.SizeOfInBytes();
            
            ExceptionHelper.ThrowByCondition(indexBufferData.Length, _IndexBufferDataException, e => e % stride != 0);
            IndexBufferData = indexBufferData;
            Format = format;
            IndexCount = indexBufferData.Length / stride;
        }

        public byte[] IndexBufferData { get; private set; } = new byte[0];
        public Format Format { get; private set; }
        public int IndexCount { get; private set; }
        public override DrawMode DrawMode => DrawMode.Indexed;
        private Buffer _IndexBuffer;
        private const string _IndexBufferDataException = "Index buffer data length must be a multiple of format size";

        internal override void Draw(DeviceContext context)
        {
            context.InputAssembler.SetVertexBuffers(0, VertexBufferBinding);
            context.InputAssembler.SetIndexBuffer(_IndexBuffer, Format, 0);
            context.DrawIndexed(IndexCount, 0, 0);
        }

        internal override void InitializeBuffers(Device device)
        {
            base.InitializeBuffers(device);
            _IndexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, IndexBufferData, IndexBufferData.Length);
        }

        internal override void DisposeBuffersData()
        {
            base.DisposeBuffersData();
            IndexBufferData = null;
        }

        internal override void Dispose()
        {
            base.Dispose();
            _IndexBuffer?.Dispose();
        }
    }
}
