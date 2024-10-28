using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using System.Runtime.InteropServices;

namespace DirectXEngine
{
    public abstract class RendererGraphicsSettings
    {
        public RendererGraphicsSettings(byte[] vertexBufferData, int vertexBufferStride)
        {
            ExceptionHelper.ThrowIfNull(vertexBufferData);
            ExceptionHelper.ThrowIfOutOfRange(vertexBufferStride, 0, double.MaxValue);
            ExceptionHelper.ThrowByCondition(vertexBufferData.Length, _VertexBufferDataException, e => e % vertexBufferStride != 0);

            VertexBufferData = vertexBufferData;
            VertexBufferStride = vertexBufferStride;
            VerticesCount = vertexBufferData.Length / vertexBufferStride;
        }

        public static NonIndexedNonInstancedDraw DefaultDraw(byte[] vertexBufferData, int vertexBufferStride) => 
            new NonIndexedNonInstancedDraw(vertexBufferData, vertexBufferStride);

        public static IndexedDraw IndexedDraw(byte[] vertexBufferData, byte[] indexBufferData,
            int vertexBufferStride, Format format) => 
            new IndexedDraw(vertexBufferData, vertexBufferStride, indexBufferData, format);

        public abstract DrawMode DrawMode { get; }
        public byte[] VertexBufferData { get; private set; } = new byte[0];
        public int VertexBufferStride { get; private set; }
        public int VerticesCount { get; private set; }
        internal protected VertexBufferBinding VertexBufferBinding { get; private set; }
        private const string _VertexBufferDataException = "Vertex buffer data length must a multiple of vertex buffer stride";
        private Buffer _VertexBuffer;

        internal abstract void Draw(DeviceContext context);

        internal virtual void InitializeBuffers(Device device)
        {
            _VertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, VertexBufferData, VertexBufferData.Length);
            VertexBufferBinding = new VertexBufferBinding(_VertexBuffer, VertexBufferStride, 0);
        }

        internal virtual void DisposeBuffersData()
        {
            VertexBufferData = null;
        }

        internal virtual void Dispose()
        {
            _VertexBuffer?.Dispose();
        }
    }
}
