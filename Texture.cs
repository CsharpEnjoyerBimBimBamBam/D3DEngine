using SharpDX.Direct3D11;
using System.Drawing;
using System.Drawing.Imaging;
using SharpDX.WIC;
using SharpDX.DXGI;
using SharpDX;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using SharpDX.Direct3D;
using System.Windows.Forms;
using System.IO;

namespace DirectXEngine
{
    public class Texture : IShaderResource
    {
        public Texture(Texture2D texture)
        {
            ExceptionHelper.ThrowIfNull(texture);
            RawTexture = texture;
        }

        public Texture(Texture2D texture, Image image) : this(texture)
        {
            ExceptionHelper.ThrowIfNull(image);
            Image = image;
        }

        public TextureAddressMode AddressU { get; set; } = TextureAddressMode.Wrap;
        public TextureAddressMode AddressV { get; set; } = TextureAddressMode.Wrap;
        public Image Image { get; }
        internal Texture2D RawTexture { get; }

        public static Texture FromImagePath(string path)
        {
            ImagingFactory imagingFactory = new ImagingFactory();
            
            BitmapDecoder bitmapDecoder = new BitmapDecoder(imagingFactory, path, DecodeOptions.CacheOnDemand);
            BitmapFrameDecode frame = bitmapDecoder.GetFrame(0);

            FormatConverter formatConverter = new FormatConverter(imagingFactory);
            formatConverter.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppRGBA);

            int width = formatConverter.Size.Width;
            int height = formatConverter.Size.Height;

            var stride = width * 4;
            var bufferSize = stride * height;
            var buffer = new byte[bufferSize];

            formatConverter.CopyPixels(buffer, stride);

            Texture2DDescription textureDesc = new Texture2DDescription
            {
                Width = width,
                Height = height,
                ArraySize = 1,
                MipLevels = 1,
                Format = Format.R8G8B8A8_UNorm,
                Usage = ResourceUsage.Immutable,
                SampleDescription = new SampleDescription(1, 0),
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            DataStream stream = new DataStream(bufferSize, true, true);
            stream.Write(buffer, 0, buffer.Length);
            stream.Position = 0;

            DataBox dataBox = new DataBox(stream.DataPointer, stride, 0);
            Texture2D texture = new Texture2D(EngineCore.Current.Device, textureDesc, new[] { dataBox });

            formatConverter.Dispose();
            frame.Dispose();
            bitmapDecoder.Dispose();
            imagingFactory.Dispose();
            stream.Dispose();

            return new Texture(texture);
        }

        public static Texture FromImage(Image image)
        {
            int width = image.Width;
            int height = image.Height;
            
            var stride = width * 4;
            var bufferSize = stride * height;

            DataStream dataStream = new DataStream(bufferSize, true, true);
            image.Save(dataStream, image.RawFormat);
            dataStream.Position = 0;

            Texture2DDescription textureDescription = new Texture2DDescription
            {
                Width = image.Width,
                Height = image.Height,
                ArraySize = 1,
                MipLevels = 1,
                Format = Format.R8G8B8A8_UNorm,
                Usage = ResourceUsage.Immutable,
                SampleDescription = new SampleDescription(1, 0),
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            DataBox dataBox = new DataBox(dataStream.DataPointer, stride, 0);
            Texture2D texture = new Texture2D(EngineCore.Current.Device, textureDescription, new[] { dataBox });

            dataStream.Dispose();

            return new Texture(texture, image);
        }

        public ShaderResource ToShaderResource(int slot, bool disposeAfterSet)
        {
            ShaderResourceViewDescription description = new ShaderResourceViewDescription
            {
                Format = Format.R8G8B8A8_UNorm,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource
                {
                    MipLevels = 1,
                    MostDetailedMip = 0
                },
            };
            
            return new ShaderResource(RawTexture, slot, disposeAfterSet, description);
        }

        public void Dispose()
        {
            RawTexture.Dispose();
        }

        internal ShaderSampler GetSampler(int slot) => new ShaderSampler(new SamplerStateDescription()
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = AddressU,
            AddressV = AddressV,
            AddressW = TextureAddressMode.Mirror,
            BorderColor = new SharpDX.Mathematics.Interop.RawColor4(1, 1, 1, 1),
        }, slot);
    }
}
