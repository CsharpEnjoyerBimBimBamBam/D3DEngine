using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.D3DCompiler;
using System.Drawing;
using System;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Reflection;

namespace DirectXEngine
{
    public class MeshRenderer : Renderer, ICloneableComponent
    {
        public MeshRenderer(GameObject attachedGameObject) : base(attachedGameObject)
        {
            Material.Shader = _MeshShader;
        }

        public Mesh Mesh
        {
            get => _Mesh;
            set
            {
                ExceptionHelper.ThrowIfNull(value);
                _Mesh = value;

                if (GameObject.IsInstantiated)
                    UpdateGraphicsSettings(GetUpdate(value));
            }
        }
        public bool UseFlatShading
        {
            get => _UseFlatShading;
            set
            {
                if (value == _UseFlatShading)
                    return;

                _UseFlatShading = value;

                if (_Mesh == null)
                    return;

                if (GameObject.IsInstantiated)
                    UpdateGraphicsSettings(GetUpdate(_Mesh));
            }
        }
        private bool _UseFlatShading = false;
        private Mesh _Mesh;
        private readonly int _VertexBufferStride = (Utilities.SizeOf<Vector3>() * 2) + Utilities.SizeOf<Vector2>();
        private readonly int _Vector3Size = Utilities.SizeOf<Vector3>();
        private readonly int _Vector2Size = Utilities.SizeOf<Vector2>();
        private static ShaderResource _DirectionalLightsInput;
        private static ShaderResource _PointLightsInput;
        private static ShaderResource _SpotlightsInput;
        private static bool _IsUpdateInitialized = false;
        private static bool _IsStartInitialized = false;
        private static ShaderSampler _ShadowMapSampler = GetShadowMapSamler();
        private ShaderSampler _TextureSampler;
        private static List<MeshRenderer> _MeshRenderers = new List<MeshRenderer>();
        private static Shader _MeshShader = CreateMeshShader();
        private ShaderResource _Texture;

        public Component Clone()
        {
            MeshRenderer componentCopy = MemberwiseClone() as MeshRenderer;
            componentCopy._Mesh = Mesh?.Clone() as Mesh;
            return componentCopy;
        }

        protected override bool NeedToDraw(Camera camera) => camera.Frustum.CheckForIntersection(Transform, _Mesh.Bounds);

        protected override ShaderDynamicResources GetResources(Camera camera)
        {
            Matrix localToWorld = Transform.LocalToWorldMatrix;

            ConstantBufferInput input = new ConstantBufferInput
            {
                ModelViewProjection = localToWorld * camera.WorldToScreenMatrix,
                ModelLocalToWorldDirection = Transform.RotationMatrix,
                ModelLocalToWorld = localToWorld,
                CameraPosition = camera.Transform.WorldPosition,
                CameraForward = camera.Transform.Forward,
                GlobalIllumination = Light.GlobalIllumination,
                IsHaveTexture = _Texture.IsDataValid ? 1 : 0,
            };
            
            ShaderResource[] shaderResource = new ShaderResource[]
            {
                _PointLightsInput,
                _DirectionalLightsInput,
                _SpotlightsInput,
                Light.GetShadowMap<PointLight>().ToShaderResource(3, false),
                Light.GetShadowMap<DirectionalLight>().ToShaderResource(4, false),
                Light.GetShadowMap<Spotlight>().ToShaderResource(5, false),
                _Texture
            };

            ShaderSampler[] samplers = new ShaderSampler[]
            {
                _ShadowMapSampler,
                _TextureSampler
            };

            byte[] constantBufferData = EngineUtilities.ToByteArray(ref input);
            return new ShaderDynamicResources(constantBufferData, shaderResource, samplers);
        }

        protected override void OnStart()
        {
            if (_Mesh != null)
            {
                UpdateGraphicsSettings(GetUpdate(_Mesh));
                //_Mesh = null;
            }
            
            _MeshRenderers.Add(this);
            
            UpdateTexture(Material.Texture);
            Material.TextureChanged += UpdateTexture;

            if (!_IsStartInitialized)
                _IsStartInitialized = true;
        }

        protected override void OnUpdate()
        {
            if (!_IsUpdateInitialized)
            {
                OnFrameRenderStart();
                _IsUpdateInitialized = true;
            }
        }

        protected override void OnEnd() => _IsUpdateInitialized = false;

        protected override void OnRemove() => RemoveFromRender();

        protected override void OnDestroy() => RemoveFromRender();

        private void UpdateTexture(Texture texture)
        {
            _Texture?.Dispose();
            _Texture = texture != null ? texture.ToShaderResource(6, false) : ShaderResource.Invalid;
            _TextureSampler = texture != null ? texture.GetSampler(1) : new ShaderSampler(new SamplerStateDescription()
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
                BorderColor = new SharpDX.Mathematics.Interop.RawColor4(1, 1, 1, 1),
            }, 1);
        }

        private static void OnFrameRenderStart()
        {
            _PointLightsInput?.Dispose();
            _DirectionalLightsInput?.Dispose();
            _SpotlightsInput?.Dispose();

            IReadOnlyList<PointLight> pointLights = PointLight.PointLights;
            IReadOnlyList<DirectionalLight> directionalLights = DirectionalLight.DirectionalLights;
            IReadOnlyList<Spotlight> spotlights = Spotlight.Spotlights;
            
            _PointLightsInput = Light.ToShaderResource(pointLights, 0);
            _DirectionalLightsInput = Light.ToShaderResource(directionalLights, 1);
            _SpotlightsInput = Light.ToShaderResource(spotlights, 2);

            Light.WriteShadowMapsInTexture(pointLights, _MeshRenderers);
            Light.WriteShadowMapsInTexture(directionalLights, _MeshRenderers);
            Light.WriteShadowMapsInTexture(spotlights, _MeshRenderers);
        }

        private static ShaderSampler GetShadowMapSamler()
        {
            SamplerStateDescription description = new SamplerStateDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
                BorderColor = new SharpDX.Mathematics.Interop.RawColor4(1, 1, 1, 1),
                MaximumAnisotropy = 0,
            };

            return new ShaderSampler(description, 0);
        }

        private RendererGraphicsSettings GetUpdate(Mesh mesh)
        {
            byte[] indexBufferData = EngineUtilities.ToByteArray(mesh.Triangles);
            Format format = Format.R32_UInt;
            
            if (_UseFlatShading)
                return RendererGraphicsSettings.DefaultDraw(GetVertexBufferDataForFlatShading(mesh), _VertexBufferStride);

            return RendererGraphicsSettings.IndexedDraw(GetVertexBufferData(mesh), indexBufferData, _VertexBufferStride, format);
        }

        private void FilVertexBufferData(ref int index, byte[] vertexBufferData, byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                vertexBufferData[index] = data[i];
                index++;
            }
        }

        private byte[] GetVertexBufferData(Mesh mesh)
        {
            int verticesCount = mesh.Vertices.Length;

            byte[] vertexBufferData = new byte[_VertexBufferStride * verticesCount];

            int index = 0;

            for (int i = 0; i < verticesCount; i++)
            {
                byte[] verticesBytes = EngineUtilities.ToByteArray(ref mesh.Vertices[i]);
                byte[] normalsBytes = null;
                byte[] uvsBytes = null;

                if (i >= mesh.Normals.Length)
                    normalsBytes = new byte[_Vector3Size];
                else
                    normalsBytes = EngineUtilities.ToByteArray(ref mesh.Normals[i]);

                if (i >= mesh.UVs.Length)
                    uvsBytes = new byte[_Vector2Size];
                else
                    uvsBytes = EngineUtilities.ToByteArray(ref mesh.UVs[i]);
                
                FilVertexBufferData(ref index, vertexBufferData, verticesBytes);
                FilVertexBufferData(ref index, vertexBufferData, normalsBytes);
                FilVertexBufferData(ref index, vertexBufferData, uvsBytes);
            }
            return vertexBufferData;
        }

        private byte[] GetVertexBufferDataForFlatShading(Mesh mesh)
        {
            int trianglesCount = mesh.Triangles.Length;
            byte[] vertexBufferData = new byte[_VertexBufferStride * trianglesCount];

            int index = 0;
            int trianglePosition = 0;

            Vector3[] vertices = mesh.Vertices;
            int[] triangles = mesh.Triangles;

            for (int i = 0; i < trianglesCount; i++)
            {
                int triangleIndex = triangles[i];
                byte[] verticesBytes = EngineUtilities.ToByteArray(ref mesh.Vertices[triangleIndex]);

                int triangleIndex1 = triangles[i - trianglePosition];
                int triangleIndex2 = triangles[i - (trianglePosition - 1)];
                int triangleIndex3 = triangles[i - (trianglePosition - 2)];

                Vector3 vertex1 = vertices[triangleIndex1];
                Vector3 vertex2 = vertices[triangleIndex2];
                Vector3 vertex3 = vertices[triangleIndex3];

                Vector3 normal = Vector3.Cross(vertex3 - vertex2, vertex1 - vertex2);
                normal.Normalize();

                byte[] normalBytes = EngineUtilities.ToByteArray(ref normal);

                byte[] uvsBytes = null;

                if (i >= mesh.UVs.Length)
                    uvsBytes = new byte[_Vector2Size];
                else
                    uvsBytes = EngineUtilities.ToByteArray(ref mesh.UVs[triangleIndex]);

                FilVertexBufferData(ref index, vertexBufferData, verticesBytes);
                FilVertexBufferData(ref index, vertexBufferData, normalBytes);
                FilVertexBufferData(ref index, vertexBufferData, uvsBytes);

                trianglePosition++;

                if (trianglePosition > 2)
                    trianglePosition = 0;
            }
            return vertexBufferData;
        }

        private static Shader CreateMeshShader()
        {
            InputElement[] inputElements = new InputElement[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 0),
                new InputElement("UV", 0, Format.R32G32_Float, 0),
            };

            int constantBufferSize = Utilities.SizeOf<ConstantBufferInput>();

            Shader meshShader = new Shader(inputElements, PrimitiveTopology.TriangleList, constantBufferSize);

            ShaderData vertexShader = new ShaderData(ShaderProfile.vs_5_0, "VS");
            ShaderData pixelShader = new ShaderData(ShaderProfile.ps_5_0, "PS");

            string shaderPath = "C:\\sisharp\\DirectXEngine\\DirectXEngine\\Shaders\\MeshShader.fx";

            vertexShader.CompileFromFile(shaderPath);
            pixelShader.CompileFromFile(shaderPath);

            meshShader.VertexShader = vertexShader;
            meshShader.PixelShader = pixelShader;

            return meshShader;
        }
    }
}
