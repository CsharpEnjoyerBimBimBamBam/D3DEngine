using SharpDX.Direct3D11;

namespace DirectXEngine
{
    public class ShaderSampler
    {
        public ShaderSampler(SamplerStateDescription description, int slot)
        {
            Description = description;
            Slot = slot;
        }

        public SamplerStateDescription Description { get; }
        public int Slot { get; }

        internal void Set(Device device)
        {
            SamplerState sampler = new SamplerState(device, Description);
            device.ImmediateContext.VertexShader.SetSampler(Slot, sampler);
            device.ImmediateContext.PixelShader.SetSampler(Slot, sampler);
            sampler.Dispose();
        }
    }
}
