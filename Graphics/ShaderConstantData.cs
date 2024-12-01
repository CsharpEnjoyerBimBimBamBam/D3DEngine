using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX.Direct3D;
using System;

namespace DirectXEngine
{
    public class ShaderConstantData : IDisposable
    {
        public ShaderConstantData(Shader shader, byte[] constantBufferData)
        {
            Initialize(shader);
            Buffer constantBuffer = Buffer.Create(_Device, BindFlags.ConstantBuffer, constantBufferData);
            ConstantBuffer = constantBuffer;
        }

        public ShaderConstantData(Shader shader, int constantBufferSize)
        {
            Initialize(shader);
            Buffer constantBuffer = new Buffer(_Device, constantBufferSize, ResourceUsage.Default, 
                BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            ConstantBuffer = constantBuffer;
        }

        public PrimitiveTopology Topology { get; private set; }
        public InputLayout Layout { get; private set; }
        public Buffer ConstantBuffer { get; private set; }
        public VertexShader VertexShader { get; private set; }
        public GeometryShader GeometryShader { get; private set; }
        public PixelShader PixelShader { get; private set; }
        private Device _Device => EngineCore.Current.Device;

        internal void UpdateConstantBuffer<T>(T[] data) where T : struct => EngineCore.Current.DeviceContext.UpdateSubresource(data, ConstantBuffer);

        public void Dispose()
        {
            Layout?.Dispose();
            ConstantBuffer?.Dispose();
            VertexShader?.Dispose();
            GeometryShader?.Dispose();
            PixelShader?.Dispose();
        }

        private void Initialize(Shader shader)
        {
            InputLayout layout = new InputLayout(_Device, shader.VertexShader.Signature, shader.VertexShaderInput);

            Topology = shader.Topology;
            Layout = layout;
            if (shader.VertexShader != null)
                VertexShader = new VertexShader(_Device, shader.VertexShader.ByteCode);
            if (shader.GeometryShader != null)
                GeometryShader = new GeometryShader(_Device, shader.GeometryShader.ByteCode);
            if (shader.PixelShader != null)
                PixelShader = new PixelShader(_Device, shader.PixelShader.ByteCode);
        }
    }
}
