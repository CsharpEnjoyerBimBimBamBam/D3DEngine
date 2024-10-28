using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Resource = SharpDX.Direct3D11.Resource;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using System;
using System.Drawing;
using System.Runtime.Remoting.Contexts;
using System.Collections.Generic;
using SharpDX;

namespace DirectXEngine
{
    public class ShaderResource : IDisposable
    {
        public ShaderResource(byte[] data, int stride, int slot, bool DisposeAfterSet = true, ShaderResourceViewDescription? description = null)
        {
            ExceptionHelper.ThrowIfNull(data);
            ExceptionHelper.ThrowByCondition(data.Length % stride != 0, _StrideException);
            _Data = data;
            Stride = stride;
            Slot = slot;
            Description = description;
            _DisposeAfterSet = DisposeAfterSet;
            IsDataValid = _Data.Length != 0;
        }

        public ShaderResource(Resource resource, int slot, bool DisposeAfterSet = true, ShaderResourceViewDescription? description = null)
        {
            ExceptionHelper.ThrowIfNull(resource);
            _Resource = resource;
            Slot = slot;
            IsDataValid = true;
            Description = description;
            _DisposeAfterSet = DisposeAfterSet;
        }

        public int Stride { get; }
        public int Slot { get; }
        public bool IsDataValid { get; } = false;
        internal static ShaderResource Invalid => new ShaderResource(new byte[0], 1, 0);
        internal ShaderResourceViewDescription? Description { get; }
        private Resource _Resource;
        private ShaderResourceView _ShaderResourceView;
        private byte[] _Data;
        private bool _DisposeAfterSet;
        private const string _StrideException = "Data length must be a multiple of stride";

        public ShaderResource Create<T>(T[] data, int slot, bool DisposeAfterSet = true, ShaderResourceViewDescription? description = null) 
            where T : struct
        {
            byte[] dataBytes = EngineUtilities.ToByteArray(data);
            int stride = Utilities.SizeOf<T>();
            return new ShaderResource(dataBytes, stride, slot, DisposeAfterSet, description);
        }

        internal void Set(Device device)
        {
            if (!IsDataValid)
                return;

            Resource resource = GetResource(device);

            if (_ShaderResourceView == null)
            {
                if (Description != null)
                    _ShaderResourceView = new ShaderResourceView(device, resource, (ShaderResourceViewDescription)Description);
                else
                    _ShaderResourceView = new ShaderResourceView(device, resource);
            }

            SetResourceView(device, _ShaderResourceView);

            if (_DisposeAfterSet)
                Dispose();
        }

        public void Dispose()
        {
            _Resource?.Dispose();
            _ShaderResourceView?.Dispose();
            _Data = null;
        }

        private Resource GetResource(Device device)
        {
            if (_Resource != null)
                return _Resource;

            _Resource = Buffer.Create(device, BindFlags.ShaderResource, _Data,
                optionFlags: ResourceOptionFlags.BufferStructured, structureByteStride: Stride);

            return _Resource;
        }

        private void SetResourceView(Device device, ShaderResourceView resourceView)
        {
            device.ImmediateContext.VertexShader.SetShaderResource(Slot, resourceView);
            device.ImmediateContext.PixelShader.SetShaderResource(Slot, resourceView);
        }
    }
}
