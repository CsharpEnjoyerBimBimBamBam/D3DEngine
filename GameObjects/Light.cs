using Assimp;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using Color = SharpDX.Color;

namespace DirectXEngine
{
    public abstract class Light : Startable, IShaderResource
    {
        protected Light(GameObject attachedGameObject) : base(attachedGameObject)
        {
            _Type = GetType();
        }

        public Color Color { get; set; } = Color.White;
        public bool CastShadows { get; set; } = true;
        public static float GlobalIllumination
        {
            get => _GlobalIllumination;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange01(value);
                _GlobalIllumination = value;
            }
        }
        protected internal abstract byte[] ShaderResourceData { get; }
        protected static Shader ShadowMapShader { get; } = CreateShadowMapShader();
        protected Camera Camera
        {
            get
            {
                if (_Camera == null)
                    _Camera = CreateDefaultCamera();
                return _Camera;
            }
        }
        protected BaseLightInput BaseInput => new BaseLightInput
        {
            Color = Color,
            CastShadows = CastShadows ? 1 : 0,
        };
        protected ShadowMap ShadowMap
        {
            get
            {
                if (_ShadowMap == null)
                    _ShadowMap = _LightsShadowMap[_Type];
                return _ShadowMap;
            }
        }
        protected DepthStencilView[] DepthViews
        {
            get
            {
                if (_DepthViews == null)
                    _DepthViews = CreateDepthStencilViews();
                return _DepthViews;
            }
        }
        protected int ShadowMapTexturesCount
        {
            get => _ShadowMapTexturesCount;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange(value, 1, double.PositiveInfinity);
                _ShadowMapTexturesCount = value;
                UpdateShadowMap(_Type);
            }
        }
        protected int StartTextureIndex { get; private set; } = 0;
        private int _ShadowMapTexturesCount = 1;
        private ShadowMap _ShadowMap;
        private DepthStencilView[] _DepthViews;
        private Camera _Camera;
        private Type _Type;
        private static List<Light> _AllLights = new List<Light>();
        private static float _GlobalIllumination = 0.3f;
        private static Dictionary<Type, ShadowMap> _LightsShadowMap = new Dictionary<Type, ShadowMap>();
        private static Dictionary<Type, Size> _LightsResoution = new Dictionary<Type, Size>();
        private static readonly Size _DefaultResolution = new Size(4096, 4096);

        public abstract void WriteShadowMapInTexture(IReadOnlyList<MeshRenderer> meshRenderers);

        public ShaderResource ToShaderResource(int slot, bool disposeAfterSet)
        {
            byte[] data = ShaderResourceData;
            return new ShaderResource(data, data.Length, slot, disposeAfterSet);
        }

        public void Dispose()
        {
            _DepthViews?.SafetyForEach(x => x?.Dispose());
        }

        internal static void UpdateShadowMapResolution<T>(Size resolution) where T : Light => 
            UpdateShadowMapResolution(typeof(T), resolution);

        internal static void UpdateShadowMapResolution(Type lightType, Size resolution)
        {
            ValidateLightType(lightType);

            if (_LightsResoution.TryGetValue(lightType, out Size size) && size == resolution)
                return;

            _LightsResoution[lightType] = resolution;
            UpdateShadowMap(lightType);
        }

        internal static ShadowMap GetShadowMap<T>() where T : Light
        {
            Type lightType = typeof(T);
            ValidateLightType(lightType);

            if (_LightsShadowMap.TryGetValue(lightType, out ShadowMap shadowMap))
                return shadowMap;

            UpdateShadowMap(lightType);

            return _LightsShadowMap[lightType];
        }

        internal static ShaderResource ToShaderResource<T>(IReadOnlyList<T> lights, int slot) where T : Light
        {
            ValidateLightType<T>();
            if (lights.Count == 0)
                return ShaderResource.Invalid;

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

        internal static void WriteShadowMapsInTexture<T>(IReadOnlyList<T> lights, IReadOnlyList<MeshRenderer> meshRenderers) where T : Light
        {
            ValidateLightType<T>();
            foreach (Light light in lights)
            {
                if (!light.CastShadows)
                    continue;

                light.WriteShadowMapInTexture(meshRenderers);
            }
        }

        protected override void OnStart()
        {
            _AllLights.Add(this);
            UpdateShadowMap(GetType());
        }

        protected override void OnDestroy()
        {
            _AllLights.Remove(this);
            UpdateShadowMap(GetType());
        }

        protected override void OnRemove()
        {
            OnDestroy();
        }

        protected void UpdateShadowMapResolution(Size resolution) => UpdateShadowMapResolution(_Type, resolution);

        protected void UpdateGraphicsDepthBias(int depthBias)
        {
            Graphics graphics = Camera.Graphics;
            RasterizerStateDescription rasterizerDescription = graphics.RasterizerDescription;
            rasterizerDescription.DepthBias = depthBias;
            graphics.RasterizerDescription = rasterizerDescription;
            graphics.OutputMode = OutputMode.DepthBuffer;
        }

        protected virtual DepthStencilView[] CreateDepthStencilViews()
        {
            DepthStencilView[] depthViews = new DepthStencilView[ShadowMapTexturesCount];

            for (int i = 0; i < ShadowMapTexturesCount; i++)
            {
                DepthStencilView depthView = new DepthStencilView(EngineCore.Current.Device, ShadowMap.Textures.RawTexture, new DepthStencilViewDescription
                {
                    Format = Format.D32_Float,
                    Dimension = DepthStencilViewDimension.Texture2DArray,
                    Texture2DArray = new DepthStencilViewDescription.Texture2DArrayResource
                    {
                        ArraySize = 1,
                        FirstArraySlice = StartTextureIndex + i,
                        MipSlice = 0,
                    }
                });

                depthViews[i] = depthView;
            }

            return depthViews;
        }

        private Camera CreateDefaultCamera()
        {
            Camera camera = Scene.Current.Instantiate<Camera>();
            camera.Graphics.OutputMode = OutputMode.DepthBuffer;
            return camera;
        }

        private static void UpdateShadowMap(Type lightType)
        {
            if (_LightsShadowMap.TryGetValue(lightType, out ShadowMap shadowMap))
                shadowMap?.Dispose();

            int texturesCount = 0;
            foreach (Light light in _AllLights)
            {
                if (!light._Type.Equals(lightType))
                    continue;

                light.StartTextureIndex = texturesCount;
                light.Dispose();
                light._DepthViews = null;
                light._ShadowMap = null;
                texturesCount += light.ShadowMapTexturesCount;
            }

            Size resolution = GetDictionaryValue(_LightsResoution, lightType, () => _DefaultResolution);
            texturesCount = texturesCount > 0 ? texturesCount : 1;
            _LightsShadowMap[lightType] = new ShadowMap(texturesCount, resolution);
        }

        private static TValue GetDictionaryValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> defaultValue)
        {
            if (dictionary.TryGetValue(key, out TValue value))
                return value;

            value = defaultValue.Invoke();
            dictionary[key] = value;
            return value;
        }

        private static void ValidateLightType<T>() => ValidateLightType(typeof(T));

        private static void ValidateLightType(Type lightType) => 
            ExceptionHelper.ThrowByCondition(lightType.Equals(typeof(Light)), "Type must be subclass of Light");

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

            return shader;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 16)]
        protected internal struct ConstantBufferData
        {
            public Matrix LightViewProjection;
            public float FarClipPlane;
            public int TransformZ;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 16)]
        protected internal struct BaseLightInput
        {
            public RawColor4 Color;
            public int CastShadows;
        }
    }
}
