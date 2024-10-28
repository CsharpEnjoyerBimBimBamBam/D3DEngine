using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DirectXEngine
{
    public class ShadowMap : IDisposable
    {
        public ShadowMap(IReadOnlyList<Texture2D> directionalLightShadowMap, Size resolution)
        {
            ExceptionHelper.ThrowIfNull(directionalLightShadowMap);

            DirectionalLightTexturesCount = directionalLightShadowMap.Count;

            Texture2DDescription textureArrayDesc = new Texture2DDescription
            {
                Width = resolution.Width,
                Height = resolution.Height,
                MipLevels = 1,
                Format = Format.R32_Float,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            };
            
            DirectionalLightShadowMap = TextureUtilities.CreateTextureArray(directionalLightShadowMap.ToArray(), textureArrayDesc);

            _DirectionalLightShadowMap = directionalLightShadowMap;
        }

        public ShadowMap(IReadOnlyList<Texture2D> directionalLightShadowMap, Size resolution, int slot) : 
            this(directionalLightShadowMap, resolution)
        {
            Resource = ToShaderResource(slot);
        }

        public Texture2D DirectionalLightShadowMap { get; private set; }
        public int DirectionalLightTexturesCount { get; }
        public ShaderResource Resource { get; private set; }
        private IEnumerable<Texture2D> _DirectionalLightShadowMap;

        public ShaderResource ToShaderResource(int slot)
        {
            if (DirectionalLightTexturesCount == 0)
                return ShaderResource.Invalid;

            ShaderResourceViewDescription description = new ShaderResourceViewDescription
            {
                Format = Format.R32_Float,
                Dimension = ShaderResourceViewDimension.Texture2DArray,
                Texture2DArray = new ShaderResourceViewDescription.Texture2DArrayResource
                {
                    MipLevels = 1,
                    ArraySize = DirectionalLightTexturesCount
                }
            };

            return new ShaderResource(DirectionalLightShadowMap, slot, false, description);
        }

        public void Dispose()
        {
            DirectionalLightShadowMap?.Dispose();
            Resource?.Dispose();
            DirectionalLightShadowMap = null;
            Resource = null;

            foreach (Texture2D texture in _DirectionalLightShadowMap)
                texture?.Dispose();
        }
    }
}
