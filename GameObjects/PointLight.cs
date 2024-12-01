using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DirectXEngine
{
    public class PointLight : Light
    {
        public PointLight(GameObject attachedGameObject) : base(attachedGameObject)
        {

        }

        public float Range
        {
            get => _Range;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange(value, 0, double.PositiveInfinity);
                _Range = value;
                Camera.FarClipPlane = value;
                UpdateProjectionMatrix();
            }
        }
        public float Diffusion
        {
            get => _Diffusion;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange(value, 0, double.PositiveInfinity);
                _Diffusion = value;
            }
        }
        public float Intensity
        {
            get => _Intensity;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange(value, 0, double.PositiveInfinity);
                _Intensity = value;
            }
        }
        public static IReadOnlyList<PointLight> PointLights => _PointLights;
        protected internal override byte[] ShaderResourceData
        {
            get
            {
                PointLightInput input = new PointLightInput
                {
                    BaseInput = BaseInput,
                    WorldPosition = Transform.WorldPosition,
                    Range = _Range,
                    Diffusion = _Diffusion,
                    Intensity = _Intensity,
                    FacesMatrices = _FacesMatrices,
                };

                return EngineUtilities.ToByteArray(ref input);
            }
        }
        private float _Range = 10;
        private Matrix _ProjectionMatrix;
        private FaceMatrix[] _FacesMatrices = new FaceMatrix[_CubeFacesCount];
        private float _Diffusion = 1f;
        private float _Intensity = 1f;
        private static readonly float _FieldOfView = MathUtil.Pi / 2;
        private const float _ZNear = 0.1f;
        private const int _CubeFacesCount = 6;
        private static List<PointLight> _PointLights = new List<PointLight>();
        private static readonly int _ConstantBufferSize = EngineUtilities.GetAlignedSize<ConstantBufferData>();
        private static ShaderConstantData _ConstantData = new ShaderConstantData(ShadowMapShader, _ConstantBufferSize);

        public override void WriteShadowMapInTexture(IReadOnlyList<MeshRenderer> meshRenderers)
        {
            UpdateFacesMatrices();
            Camera.Graphics.Resolution = ShadowMap.Textures.Resolution;
            for (int i = 0; i < _CubeFacesCount; i++)
                WriteFaceShadowMapInTexture(meshRenderers, i);
        }

        protected override void OnStart()
        {
            ShadowMapTexturesCount = _CubeFacesCount;
            base.OnStart();
            _PointLights.Add(this);
            UpdateProjectionMatrix();
            UpdateGraphicsDepthBias(100000);
            Camera.NearClipPlane = _ZNear;
            Camera.FarClipPlane = _Range;
        }

        private void WriteFaceShadowMapInTexture(IReadOnlyList<MeshRenderer> meshRenderers, int faceIndex)
        {
            Dictionary<Renderer, ManualDrawDescription> rendererDescriptions = new Dictionary<Renderer, ManualDrawDescription>();
            Matrix viewProjection = _FacesMatrices[faceIndex].ViewProjection;

            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                Matrix worldViewProjection = meshRenderer.Transform.LocalToWorldMatrix * viewProjection;

                ConstantBufferData input = new ConstantBufferData
                {
                    LightViewProjection = worldViewProjection,
                    FarClipPlane = _Range,
                    TransformZ = 1
                };

                byte[] constantBufferData = EngineUtilities.ToByteArray(ref input, true);
                rendererDescriptions[meshRenderer] = new ManualDrawDescription(_ConstantData, constantBufferData);
            }

            Camera.Graphics.DrawAll(rendererDescriptions, DepthViews[faceIndex]);
        }

        private void UpdateProjectionMatrix() => _ProjectionMatrix = Matrix.PerspectiveFovLH(_FieldOfView, 1, _ZNear, _Range);

        private void UpdateFacesMatrices()
        {
            Vector3 forward = Vector3.ForwardLH;
            Vector3 backward = Vector3.BackwardLH;
            Vector3 up = Vector3.Up;
            Vector3 down = Vector3.Down;
            Vector3 right = Vector3.Right;
            Vector3 left = Vector3.Left;

            Vector3 position = Transform.WorldPosition;

            Matrix forwardView = Matrix.LookAtLH(position, position + forward, up);
            Matrix backwardView = Matrix.LookAtLH(position, position + backward, up);
            Matrix topView = Matrix.LookAtLH(position, position + up, backward);
            Matrix bottomView = Matrix.LookAtLH(position, position + down, backward);
            Matrix rightView = Matrix.LookAtLH(position, position + right, up);
            Matrix leftView = Matrix.LookAtLH(position, position + left, up);

            Matrix.Multiply(ref forwardView, ref _ProjectionMatrix, out forwardView);
            Matrix.Multiply(ref backwardView, ref _ProjectionMatrix, out backwardView);
            Matrix.Multiply(ref topView, ref _ProjectionMatrix, out topView);
            Matrix.Multiply(ref bottomView, ref _ProjectionMatrix, out bottomView);
            Matrix.Multiply(ref rightView, ref _ProjectionMatrix, out rightView);
            Matrix.Multiply(ref leftView, ref _ProjectionMatrix, out leftView);

            _FacesMatrices[0] = new FaceMatrix(forwardView, forward);
            _FacesMatrices[1] = new FaceMatrix(leftView, left);
            _FacesMatrices[2] = new FaceMatrix(backwardView, backward);
            _FacesMatrices[3] = new FaceMatrix(rightView, right);
            _FacesMatrices[4] = new FaceMatrix(topView, up);
            _FacesMatrices[5] = new FaceMatrix(bottomView, down);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct PointLightInput
        {
            public BaseLightInput BaseInput;
            public Vector3 WorldPosition;
            public float Range;
            public float Diffusion;
            public float Intensity;
            public int StartTextureIndex;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public FaceMatrix[] FacesMatrices;
        }

        private struct FaceMatrix
        {
            public FaceMatrix(Matrix viewProjection, Vector3 direction)
            {
                ViewProjection = viewProjection;
                Direction = direction;
            }

            public Matrix ViewProjection;
            public Vector3 Direction;
        }
    }
}
