namespace DirectXEngine
{
    public class ShaderDynamicResources
    {
        public ShaderDynamicResources(byte[] constantBufferData, ShaderResource[] resources, ShaderSampler[] samplers)
        {
            ConstantBufferData = constantBufferData;
            Resources = resources;
            Samplers = samplers;
        }

        public byte[] ConstantBufferData { get; }
        public ShaderResource[] Resources { get; }
        public ShaderSampler[] Samplers { get; }
    }
}
