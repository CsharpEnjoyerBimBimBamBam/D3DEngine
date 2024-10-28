using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;

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
                    WorldPosition = Transform.WorldPosition,
                    WorldToLocal = Transform.WorldToLocalMatrix,
                    Projection = Matrix.Identity,
                    Range = _Range,
                    Diffusion = _Diffusion,
                    Intensity = _Intensity
                };

                return EngineUtilities.ToByteArray(ref input);
            }
        }
        private float _Range = 100;
        private float _Diffusion = 1f;
        private float _Intensity = 1f;
        private static List<PointLight> _PointLights = new List<PointLight>();

        public override Texture2D PrepareShadowMap(IList<MeshRenderer> meshRenderers, Size size)
        {
            throw new NotImplementedException();
        }

        private struct PointLightInput
        {
            public Vector3 WorldPosition;
            public Matrix WorldToLocal;
            public Matrix Projection;
            public float Range;
            public float Diffusion;
            public float Intensity;
        }
    }
}
