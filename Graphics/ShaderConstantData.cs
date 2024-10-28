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
        public ShaderConstantData(Device device, Shader shader, byte[] constantBufferData)
        {
            Initialize(device, shader);
            Buffer constantBuffer = Buffer.Create(device, BindFlags.ConstantBuffer, constantBufferData);
            ConstantBuffer = constantBuffer;
        }

        public ShaderConstantData(Device device, Shader shader, int constantBufferSize)
        {
            Initialize(device, shader);
            Buffer constantBuffer = new Buffer(device, constantBufferSize, ResourceUsage.Default, 
                BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            ConstantBuffer = constantBuffer;
        }

        public PrimitiveTopology Topology { get; private set; }
        public InputLayout Layout { get; private set; }
        public Buffer ConstantBuffer { get; private set; }
        public VertexShader VertexShader { get; private set; }
        public PixelShader PixelShader { get; private set; }

        public void Dispose()
        {
            Layout?.Dispose();
            ConstantBuffer?.Dispose();
            VertexShader?.Dispose();
            PixelShader?.Dispose();
        }

        private void Initialize(Device device, Shader shader)
        {
            ShaderSignature signature = new ShaderSignature(shader.VertexShader.ByteCode);
            InputLayout layout = new InputLayout(device, signature, shader.VertexShaderInput);

            Topology = shader.Topology;
            Layout = layout;
            if (shader.VertexShader != null)
                VertexShader = new VertexShader(device, shader.VertexShader.ByteCode);
            if (shader.PixelShader != null)
                PixelShader = new PixelShader(device, shader.PixelShader.ByteCode);
        }
    }
}
