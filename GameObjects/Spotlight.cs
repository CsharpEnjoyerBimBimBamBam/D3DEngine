using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DirectXEngine
{
    public class Spotlight : Light
    {
        public Spotlight(GameObject attachedGameObject) : base(attachedGameObject)
        {

        }

        public static IReadOnlyList<Spotlight> Spotlights => _Spotlights;
        public float Angle
        {
            get => _Angle;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange(value, 0, 180 - double.Epsilon);
                _Angle = value;
                _AngleRadians = MathUtil.DegreesToRadians(_Angle);
                Camera.FieldOfView = _Angle;
            }
        }
        public float Range
        {
            get => _Range;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange(value, 0, double.PositiveInfinity);
                _Range = value;
                Camera.FarClipPlane = _Range;
            }
        }
        protected internal override byte[] ShaderResourceData
        {
            get
            {
                SpotlightInput input = new SpotlightInput
                {
                    Position = Transform.WorldPosition,
                    Direction = Transform.Forward,
                    Angle = _AngleRadians / 2,
                    Range = _Range,
                    ViewProjection = ViewProjectionMatrix
                };

                return EngineUtilities.ToByteArray(ref input);
            }
        }
        private Matrix ViewProjectionMatrix => Transform.ViewMatrix * Camera.ProjectionMatrix;
        private float _Range = 15;
        private float _Angle = _DefaultAngle;
        private float _AngleRadians = MathUtil.DegreesToRadians(_DefaultAngle);
        private Graphics _Graphics;
        private static List<Spotlight> _Spotlights = new List<Spotlight>();
        private const float _DefaultAngle = 60;

        public override Texture2D PrepareShadowMap(IList<MeshRenderer> meshRenderers, Size resolution)
        {
            Dictionary<Renderer, ManualDrawDescription> rendererDescriptions = new Dictionary<Renderer, ManualDrawDescription>();
            Matrix viewProjectionMatrix = ViewProjectionMatrix;

            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                Matrix worldViewProjection = meshRenderer.Transform.LocalToWorldMatrix * viewProjectionMatrix;

                ConstantBufferData input = new ConstantBufferData
                {
                    LightViewProjection = worldViewProjection,
                    FarClipPlane = Camera.FarClipPlane,
                    TransformZ = 1
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
            _Spotlights.Add(this);

            _Graphics = Camera.Graphics;

            Camera.FieldOfView = _Angle;
            Camera.FarClipPlane = _Range;

            RasterizerStateDescription rasterizerDescription = _Graphics.RasterizerDescription;
            rasterizerDescription.DepthBias = 500000;

            _Graphics.RasterizerDescription = rasterizerDescription;
        }

        private struct SpotlightInput
        {
            public Vector3 Position;
            public Vector3 Direction;
            public float Angle;
            public float Range;
            public float MinShadowDistance;
            public Matrix ViewProjection;
        }
    }
}
