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
                    BaseInput = BaseInput,
                    Position = Transform.WorldPosition,
                    Direction = Transform.Forward,
                    Angle = _AngleRadians / 2,
                    Range = _Range,
                    ViewProjection = ViewProjectionMatrix
                };

                return EngineUtilities.ToByteArray(ref input);
            }
        }
        private Matrix ViewProjectionMatrix
        {
            get
            {
                Vector3 position = Transform.WorldPosition;
                Vector3 direction = Transform.Forward;
                return Matrix.LookAtLH(position, direction, Vector3.Up) * Camera.ProjectionMatrix;
            }
        }
        private float _Range = 50;
        private float _Angle = _DefaultAngle;
        private float _AngleRadians = MathUtil.DegreesToRadians(_DefaultAngle);
        private Graphics _Graphics;
        private static List<Spotlight> _Spotlights = new List<Spotlight>();
        private const float _DefaultAngle = 60;
        private static readonly int _ConstantBufferSize = EngineUtilities.GetAlignedSize<ConstantBufferData>();
        private static ShaderConstantData _ConstantData = new ShaderConstantData(ShadowMapShader, _ConstantBufferSize);

        public override void WriteShadowMapInTexture(IReadOnlyList<MeshRenderer> meshRenderers)
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

                byte[] constantBufferData = EngineUtilities.ToByteArray(ref input, true);
                rendererDescriptions[meshRenderer] = new ManualDrawDescription(_ConstantData, constantBufferData);
            }

            ShadowMap shadowMap = ShadowMap;
            Graphics graphics = Camera.Graphics;
            graphics.Resolution = shadowMap.Textures.Resolution;
            graphics.DrawAll(rendererDescriptions, DepthViews[0]);
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
            public BaseLightInput BaseInput;
            public Vector3 Position;
            public Vector3 Direction;
            public float Angle;
            public float Range;
            public float MinShadowDistance;
            public Matrix ViewProjection;
        }
    }
}
