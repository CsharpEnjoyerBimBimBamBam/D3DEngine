using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DirectXEngine
{
    public class DirectionalLight : Light
    {
        public DirectionalLight(GameObject attachedGameObject) : base(attachedGameObject)
        {
            CastShadow = true;
        }

        public float ProjectionOffset
        {
            get => _ProjectionOffset;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange(value, 0, double.PositiveInfinity);
                _ProjectionOffset = value;
            }
        }
        internal static IReadOnlyList<DirectionalLight> DirectionalLights => _DirectionalLights;
        protected internal override byte[] ShaderResourceData
        {
            get
            {
                Frustum[] frustums = Frustum.CalculateSubFrustums(_MainCamera, 5);

                DirectionalLightInput input = new DirectionalLightInput
                {
                    ForwardDirection = Transform.Forward,
                    ViewProjection = GetViewProjectionMatrix(frustums[0]),
                    FarClipPlane = Camera.FarClipPlane
                };

                return EngineUtilities.ToByteArray(ref input);
            }
        }
        private static List<DirectionalLight> _DirectionalLights = new List<DirectionalLight>();
        private Camera _MainCamera;
        private float _ProjectionOffset = 10;
        private Matrix _ViewProjectionMatrix = Matrix.Identity;

        public override Texture2D PrepareShadowMap(IList<MeshRenderer> meshRenderers, Size resolution)
        {
            Dictionary<Renderer, ManualDrawDescription> rendererDescriptions = new Dictionary<Renderer, ManualDrawDescription>();
            Frustum[] frustums = Frustum.CalculateSubFrustums(_MainCamera, 5);
            Matrix viewProjectionMatrix = GetViewProjectionMatrix(frustums[0]);

            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                Matrix worldViewProjection = meshRenderer.Transform.LocalToWorldMatrix * viewProjectionMatrix;

                ConstantBufferData input = new ConstantBufferData
                {
                    LightViewProjection = worldViewProjection,
                    FarClipPlane = Camera.FarClipPlane,
                    TransformZ = 0
                };

                byte[] constantBufferData = EngineUtilities.ToByteArray(ref input);               
                rendererDescriptions[meshRenderer] = new ManualDrawDescription(ShadowMapShader, constantBufferData);
            }

            Graphics graphics = Camera.Graphics;
            graphics.Resolution = resolution;
            graphics.DrawAll(rendererDescriptions);
            Texture2D depthBuffer = graphics.DepthBuffer;
            
            return depthBuffer;
        }

        protected override void OnStart()
        {
            base.OnStart();
            _DirectionalLights.Add(this);

            Camera.UsePerspective = false;

            RasterizerStateDescription rasterizerDescription = Camera.Graphics.RasterizerDescription;
            rasterizerDescription.DepthBias = 100000;

            Camera.Graphics.RasterizerDescription = rasterizerDescription;
            _MainCamera = Camera.Main;
        }

        private Matrix GetViewProjectionMatrix(Frustum frustum)
        {
            IReadOnlyList<Vector3> corners = frustum.AllCorners;

            //if (IsFrustrumInsideProjectionSpace(corners))
            //    return _ViewProjectionMatrix;

            _ViewProjectionMatrix = CalculateViewProjectionMatrix(frustum);
            return _ViewProjectionMatrix;
        }

        private Matrix CalculateViewProjectionMatrix(Frustum frustum)
        {
            Vector3 forward = Transform.Forward;
            Vector3 cameraRight = _MainCamera.Transform.Right;      
            Vector3 frustumCenter = frustum.Center;

            Vector3.Cross(ref forward, ref cameraRight, out Vector3 viewUp);
            Vector3.Add(ref frustumCenter, ref forward, out Vector3 target);
            Matrix.LookAtLH(ref frustumCenter, ref target, ref viewUp, out Matrix viewMatrix);

            Vector3 min = new Vector3(float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity);
            Vector3 transformedCorner;

            IReadOnlyList<Vector3> corners = frustum.AllCorners;

            for (int i = 0; i < corners.Count; i++)
            {
                Vector3 corner = corners[i];
                Vector3.Transform(ref corner, ref viewMatrix, out transformedCorner);
                Vector3.Min(ref min, ref transformedCorner, out min);
                Vector3.Max(ref max, ref transformedCorner, out max);
            }
            
            Vector3.Add(ref max, ref _ProjectionOffset, out max);
            Vector3.Subtract(ref min, ref _ProjectionOffset, out min);

            return viewMatrix * Matrix.OrthoOffCenterLH(min.X, max.X, min.Y, max.Y, min.Z, max.Z);
        }

        bool IsFrustrumInsideProjectionSpace(IReadOnlyList<Vector3> corners)
        {
            Vector3 transformedCorner;

            for (int i = 0; i < corners.Count; i++)
            {
                Vector3 corner = corners[i];
                Vector3.Transform(ref corner, ref _ViewProjectionMatrix, out transformedCorner);
                Vector3.Abs(ref transformedCorner, out Vector3 absTransformedCorner);

                if (absTransformedCorner.X > 1 || absTransformedCorner.Y > 1 || absTransformedCorner.Z > 1)
                    return false;
            }

            return true;
        }

        private struct DirectionalLightInput
        {
            public Vector3 ForwardDirection;
            public Matrix ViewProjection;
            public float FarClipPlane;
        }
    }
}
