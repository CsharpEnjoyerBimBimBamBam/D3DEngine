using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System.Collections.Generic;
using System.Drawing;
using Color = SharpDX.Color;
using SharpDX.DXGI;
using System.Linq;
using System.Runtime.InteropServices;

namespace DirectXEngine
{
    public abstract class Light : Updatable
    {
        protected Light(GameObject attachedGameObject) : base(attachedGameObject)
        {

        }

        public Color Color { get; set; } = Color.White;
        public bool CastShadow { get; set; }
        public static float GlobalIllumination
        {
            get => _GlobalIllumination;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange01(value);
                _GlobalIllumination = value;
            }
        }
        private static float _GlobalIllumination = 0.5f;

        public abstract Texture2D PrepareShadowMap(IList<MeshRenderer> meshRenderers, Size size);

        internal ShaderResource ToShaderResource(int slot)
        {
            byte[] data = ShaderResourceData;
            return new ShaderResource(data, data.Length, slot);
        }

        internal static ShaderResource ToShaderResource<T>(IReadOnlyList<T> lights, int slot) where T : Light
        {
            if (lights.Count == 0)
                return new ShaderResource(new byte[16], 16, slot, false);

            byte[] lightData = lights[0].ShaderResourceData;
            byte[] data = new byte[lightData.Length * lights.Count];
            int stride = lightData.Length;
            int index = 0;

            for (int i = 0; i < lightData.Length; i++)
            {
                data[index] = lightData[i];
                index++;
            }

            for (int i = 1; i < lights.Count; i++)
            {
                byte[] currentLightData = lights[i].ShaderResourceData;
                for (int j = 0; j < currentLightData.Length; j++)
                {
                    data[index] = currentLightData[j];
                    index++;
                }
            }

            return new ShaderResource(data, stride, slot, false);
        }

        protected override void OnStart()
        {
            Camera = Scene.Current.Instantiate<Camera>();
            Camera.Graphics.OutputMode = OutputMode.DepthBuffer;
        }

        protected internal abstract byte[] ShaderResourceData { get; }
        protected static Shader ShadowMapShader { get; } = CreateShadowMapShader();
        protected Camera Camera { get; private set; }

        private static Shader CreateShadowMapShader()
        {
            InputElement[] inputElements = new InputElement[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 0),
            };

            int constantBufferSize = Utilities.SizeOf<Matrix>();

            Shader shader = new Shader(inputElements, PrimitiveTopology.TriangleList, constantBufferSize);

            ShaderData vertexShader = new ShaderData(ShaderProfile.vs_5_0, "VS");

            string shaderPath = "C:\\sisharp\\DirectXEngine\\DirectXEngine\\Shaders\\ShadowMapShader.fx";

            vertexShader.CompileFromFile(shaderPath);

            shader.VertexShader = vertexShader;
            //shader.PixelShader = pixelShader;

            return shader;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 16)]
        protected internal struct ConstantBufferData
        {
            public Matrix LightViewProjection;
            public float FarClipPlane;
            public int TransformZ;
            private bool _Padding1;
            private float _Padding2;
        }
    }
}
