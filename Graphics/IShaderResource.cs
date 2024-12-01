using System;

namespace DirectXEngine
{
    internal interface IShaderResource : IDisposable
    {
        public ShaderResource ToShaderResource(int slot, bool disposeAfterSet);
    }
}
