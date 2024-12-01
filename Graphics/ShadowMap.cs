using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Resource = SharpDX.Direct3D11.Resource;

namespace DirectXEngine
{
    public class ShadowMap : IShaderResource
    {
        public ShadowMap(IReadOnlyList<Texture2D> shadowMaptextures, Size resolution)
        {
            ExceptionHelper.ThrowIfNull(shadowMaptextures);
            Textures = TextureArray.Create(shadowMaptextures, GetTextureArrayDescription(resolution));
        }

        public ShadowMap(int texturesCount, Size resolution)
        {
            Textures = TextureArray.Create(texturesCount, GetTextureArrayDescription(resolution));
        }

        public ShadowMap(IReadOnlyList<Texture2D> directionalLightShadowMap, Size resolution, int slot) : 
            this(directionalLightShadowMap, resolution)
        {
            Resource = ToShaderResource(slot, false);
        }

        public ShadowMap(int texturesCount, Size resolution, int slot) :
            this(texturesCount, resolution)
        {
            Resource = ToShaderResource(slot, false);
        }

        internal ShadowMap(TextureArray textures)
        {
            Textures = textures;
        }

        internal TextureArray Textures { get; private set; }
        public ShaderResource Resource { get; private set; }

        public ShaderResource ToShaderResource(int slot, bool disposeAfterSet)
        {
            if (Resource != null)
                return Resource;

            if (Textures == null || Textures.Count == 0)
                return ShaderResource.CreateEmpty(slot);

            Resource = Textures.ToShaderResource(slot, disposeAfterSet);
            return Resource;
        }

        public static ShadowMap FromLights(IReadOnlyList<Light> lights, Size resolution, int slot) =>
            new ShadowMap(CreateShadowMapTextures(lights, resolution), resolution, slot);

        public static ShadowMap FromLights(IReadOnlyList<Light> lights, Size resolution) =>
            new ShadowMap(CreateShadowMapTextures(lights, resolution), resolution);

        public void Dispose()
        {
            Textures?.Dispose();
            Resource?.Dispose();
            Textures = null;
            Resource = null;
        }

        private Texture2DDescription GetTextureArrayDescription(Size resolution) => new Texture2DDescription
        {
            Width = resolution.Width,
            Height = resolution.Height,
            MipLevels = 1,
            Format = Format.R32_Typeless,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        };

        private static Texture2D[] CreateShadowMapTextures(IReadOnlyList<Light> lights, Size resolution)
        {
            Texture2D[] shadowMap = new Texture2D[lights.Count(x => x.CastShadows)];
            int index = 0;

            for (int i = 0; i < lights.Count; i++)
            {
                Light light = lights[i];

                if (!light.CastShadows)
                    continue;

                shadowMap[index] = CreateTexture(resolution);
            }

            return shadowMap;
        }

        private static Texture2D CreateTexture(Size resolution) => new Texture2D(EngineCore.Current.Device, new Texture2DDescription()
        {
            Format = Format.D32_Float,
            ArraySize = 1,
            MipLevels = 1,
            Width = resolution.Width,
            Height = resolution.Height,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        });
    }
}
