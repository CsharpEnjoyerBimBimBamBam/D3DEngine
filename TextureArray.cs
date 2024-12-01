using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Collections.Generic;
using System.Drawing;

namespace DirectXEngine
{
    internal class TextureArray : IShaderResource
    {
        public TextureArray(IReadOnlyList<Texture2D> rawTextures, Texture2D textureArray, int count) : this(textureArray, count)
        {
            RawTextures = rawTextures;
            Resolution = ResolutionFromDescription(textureArray.Description);
        }

        public TextureArray(IReadOnlyList<Texture2D> rawTextures, Texture2D textureArray, int count, Format format) : this(rawTextures, textureArray, count)
        {
            Format = format;
        }

        public TextureArray(Texture2D textureArray, int count)
        {
            RawTexture = textureArray;
            Count = count;
            Resolution = ResolutionFromDescription(textureArray.Description);
        }

        public Texture2D RawTexture { get; }
        public int Count { get; }
        public Format Format { get; } = Format.R32_Float;
        public Size Resolution { get; }
        public IReadOnlyList<Texture2D> RawTextures { get; }

        public void Dispose()
        {
            RawTexture?.Dispose();

            if (RawTextures == null)
                return;

            foreach (Texture2D texture in RawTextures)
                texture?.Dispose();
        }

        public ShaderResource ToShaderResource(int slot, bool disposeAfterSet)
        {
            ShaderResourceViewDescription description = new ShaderResourceViewDescription
            {
                Format = Format,
                Dimension = ShaderResourceViewDimension.Texture2DArray,
                Texture2DArray = new ShaderResourceViewDescription.Texture2DArrayResource
                {
                    MipLevels = 1,
                    ArraySize = Count
                }
            };

            return new ShaderResource(RawTexture, slot, disposeAfterSet, description);
        }

        public void UpdateTextures(IReadOnlyList<Texture2D> textures)
        {
            ExceptionHelper.ThrowByCondition(textures.Count != Count, "Textures count must be equals");
            CopyTexturesToArray(textures, RawTexture);
        }

        public static TextureArray Create(int texturesCount, Texture2DDescription description)
        {
            ExceptionHelper.ThrowIfOutOfRange(texturesCount, 0, double.PositiveInfinity);
            texturesCount = texturesCount >= 1 ? texturesCount : 1;
            description.ArraySize = texturesCount;

            Texture2D textureArray = new Texture2D(EngineCore.Current.Device, description);
            return new TextureArray(textureArray, texturesCount);
        }

        public static TextureArray Create(IReadOnlyList<Texture2D> textures, Texture2DDescription description, Format format)
        {
            ExceptionHelper.ThrowIfNull(textures);

            int size = textures.Count;
            if (size == 0)
                size = 1;

            description.ArraySize = size;
            Texture2D textureArray = new Texture2D(EngineCore.Current.Device, description);

            CopyTexturesToArray(textures, textureArray);

            return new TextureArray(textures, textureArray, textures.Count, format);
        }

        public static TextureArray Create(IReadOnlyList<Texture2D> textures, Texture2DDescription description) => Create(textures, description, Format.R32_Float);

        public static TextureArray Create(IReadOnlyList<Texture2D> textures) => Create(textures, default);

        private Size ResolutionFromDescription(Texture2DDescription description) => new Size(description.Width, description.Height);

        private static void CopyTexturesToArray(IReadOnlyList<Texture2D> textures, Texture2D textureArray)
        {
            for (int i = 0; i < textures.Count; i++)
                CopyTextureToArray(textures[i], textureArray, i);
        }

        private static void CopyTextureToArray(Texture2D texture, Texture2D textureArray, int index) =>
            EngineCore.Current.DeviceContext.CopySubresourceRegion(texture, 0, null, textureArray, index);
    }
}
