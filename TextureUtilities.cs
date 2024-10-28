using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace DirectXEngine
{
    public static class TextureUtilities
    {
        public static Texture2D CreateTextureArray(Texture2D[] textures, Texture2DDescription description)
        {
            ExceptionHelper.ThrowIfNull(textures);

            int size = textures.Length;
            if (size == 0)
                size = 1;

            description.ArraySize = size;
            Texture2D textureArray = new Texture2D(EngineCore.Current.Device, description);

            CopyTexturesToArray(textures, textureArray);

            return textureArray;
        }

        public static Texture2D CreateTextureArray(Texture2D[] textures)
        {
            ExceptionHelper.ThrowIfNull(textures);
            Texture2DDescription description = default;

            Texture2D textureArray = new Texture2D(EngineCore.Current.Device, description);

            CopyTexturesToArray(textures, textureArray);

            return textureArray;
        }

        private static void CopyTexturesToArray(Texture2D[] textures, Texture2D textureArray)
        {
            for (int i = 0; i < textures.Length; i++)
                EngineCore.Current.DeviceContext.CopySubresourceRegion(textures[i], 0, null, textureArray, i);
        }
    }
}
