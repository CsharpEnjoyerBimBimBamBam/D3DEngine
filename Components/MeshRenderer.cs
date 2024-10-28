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

namespace DirectXEngine
{
    public class MeshRenderer : Renderer, ICloneableComponent
    {
        public MeshRenderer(GameObject attachedGameObject) : base(attachedGameObject)
        {
            InputElement[] inputElements = new InputElement[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 0),
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

            Material.Shader = meshShader;
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
        private readonly int _VertexBufferStride = Utilities.SizeOf<Vector3>() * 2;
        private readonly int _VectorSize = Utilities.SizeOf<Vector3>();
        private static ShaderResource _DirectionalLightsInput;
        private static ShaderResource _PointLightsInput;
        private static ShaderResource _SpotlightsInput;
        private static bool _IsInitialized = false;
        private static ShadowMap _DirectionalLightShadowMap;
        private static ShadowMap _PointLightShadowMap;
        private static ShadowMap _SpotlightShadowMap;
        private static ShaderSampler _ShadowMapSampler;
        private static List<MeshRenderer> _MeshRenderers = new List<MeshRenderer>();

        public Component Clone()
        {
            MeshRenderer componentCopy = MemberwiseClone() as MeshRenderer;
            componentCopy._Mesh = Mesh.Clone() as Mesh;          
            return componentCopy;
        }

        protected override ShaderDynamicResources GetResources(Camera camera)
        {
            Matrix localToWorld = Transform.LocalToWorldMatrix;

            ConstantBufferInput input = new ConstantBufferInput
            {
                ModelViewProjection = localToWorld * camera.WorldToScreenMatrix,
                ModelLocalToWorldDirection = Transform.RotationMatrix,
                ModelLocalToWorld = localToWorld,
                CameraScreenToWorld = camera.ScreenToWorldMatrix,
                CameraPosition = camera.Transform.WorldPosition.ToVector4(),
                GlobalIllumination = Light.GlobalIllumination
            };

            byte[] constantBufferData = EngineUtilities.ToByteArray(ref input);
            ShaderResource[] shaderResource = new ShaderResource[]
            {
                _PointLightsInput,
                _DirectionalLightsInput,
                _SpotlightsInput,
                _DirectionalLightShadowMap.Resource,
                _SpotlightShadowMap.Resource,
            };

            ShaderSampler[] samplers = new ShaderSampler[]
            {
                _ShadowMapSampler
            };

            return new ShaderDynamicResources(constantBufferData, shaderResource, samplers);
        }

        protected override void OnStart()
        {
            if (_Mesh != null)
                UpdateGraphicsSettings(GetUpdate(_Mesh));

            if (_ShadowMapSampler == null)
                _ShadowMapSampler = GetShadowMapSamler();

            _MeshRenderers.Add(this);
        }

        protected override void OnUpdate()
        {
            if (!_IsInitialized)
            {
                OnFrameRenderStart();
                _IsInitialized = true;
            }
        }

        protected override void OnEnd() => _IsInitialized = false;

        private void OnFrameRenderStart()
        {
            _PointLightShadowMap?.Dispose();
            _DirectionalLightShadowMap?.Dispose();
            _SpotlightShadowMap?.Dispose();

            _PointLightsInput?.Dispose();
            _DirectionalLightsInput?.Dispose();
            _SpotlightsInput?.Dispose();

            int pointLightsCount = PointLight.PointLights.Count;
            int directionalLightsCount = DirectionalLight.DirectionalLights.Count;
            int spotLightsCount = Spotlight.Spotlights.Count;
            
            IReadOnlyList<PointLight> pointLights = PointLight.PointLights;
            IReadOnlyList<DirectionalLight> directionalLights = DirectionalLight.DirectionalLights;
            IReadOnlyList<Spotlight> spotlights = Spotlight.Spotlights;

            Size resolution = new Size(4096, 4096);

            Texture2D[] directionalLightShadowMap = new Texture2D[directionalLightsCount];

            _PointLightsInput = Light.ToShaderResource(pointLights, 0);
            _DirectionalLightsInput = Light.ToShaderResource(directionalLights, 1);
            _SpotlightsInput = Light.ToShaderResource(spotlights, 2);
            
            for (int i = 0; i < directionalLightsCount; i++)
                directionalLightShadowMap[i] = directionalLights[i].PrepareShadowMap(_MeshRenderers, resolution);
            
            Texture2D[] spotlightShadowMap = new Texture2D[spotLightsCount];

            for (int i = 0; i < spotLightsCount; i++)
                spotlightShadowMap[i] = spotlights[i].PrepareShadowMap(_MeshRenderers, resolution);
            
            _DirectionalLightShadowMap = new ShadowMap(directionalLightShadowMap, resolution, 3);         
            _SpotlightShadowMap = new ShadowMap(spotlightShadowMap, resolution, 4);
        }

        private ShaderSampler GetShadowMapSamler()
        {
            SamplerStateDescription description = new SamplerStateDescription
            {
                Filter = Filter.MinMagMipPoint,
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

        private byte[] GetVertexBufferData(Mesh mesh)
        {
            int verticesCount = mesh.Vertices.Length;

            byte[] vertexBufferData = new byte[_VertexBufferStride * verticesCount];

            int index = 0;

            for (int i = 0; i < verticesCount; i++)
            {
                byte[] verticesBytes = EngineUtilities.ToByteArray(ref mesh.Vertices[i]);
                byte[] normalsBytes = null;

                if (i >= mesh.Normals.Length)
                    normalsBytes = new byte[_VectorSize];
                else
                    normalsBytes = EngineUtilities.ToByteArray(ref mesh.Normals[i]);

                for (int j = 0; j < verticesBytes.Length; j++)
                {
                    vertexBufferData[index] = verticesBytes[j];
                    index++;
                }

                for (int j = 0; j < normalsBytes.Length; j++)
                {
                    vertexBufferData[index] = normalsBytes[j];
                    index++;
                }
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
                byte[] vertexBytes = EngineUtilities.ToByteArray(ref mesh.Vertices[triangleIndex]);

                for (int j = 0; j < vertexBytes.Length; j++)
                {
                    vertexBufferData[index] = vertexBytes[j];
                    index++;
                }

                int triangleIndex1 = triangles[i - trianglePosition];
                int triangleIndex2 = triangles[i - (trianglePosition - 1)];
                int triangleIndex3 = triangles[i - (trianglePosition - 2)];

                Vector3 vertex1 = vertices[triangleIndex1];
                Vector3 vertex2 = vertices[triangleIndex2];
                Vector3 vertex3 = vertices[triangleIndex3];

                Vector3 normal = Vector3.Cross(vertex3 - vertex2, vertex1 - vertex2);
                normal.Normalize();

                byte[] normalBytes = EngineUtilities.ToByteArray(ref normal);

                for (int j = 0; j < normalBytes.Length; j++)
                {
                    vertexBufferData[index] = normalBytes[j];
                    index++;
                }

                trianglePosition++;

                if (trianglePosition > 2)
                    trianglePosition = 0;
            }
            return vertexBufferData;
        }
    }

    public abstract class ShadowCaster : Renderer
    {
        protected ShadowCaster(GameObject attachedGameObject) : base(attachedGameObject)
        {

        }

        protected override void OnInstantiate()
        {
            
        }
    }
}
