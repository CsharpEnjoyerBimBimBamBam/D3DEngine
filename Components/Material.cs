using System;
using SharpDX;

namespace DirectXEngine
{
    public class Material : Component
    {
        public Material(GameObject attachedGameObject) : base(attachedGameObject)
        {

        }

        public Shader Shader
        {
            get
            {
                if (_Shader == null)
                    _Shader = Shader.Default;
                return _Shader;
            }
            set
            {
                ExceptionHelper.ThrowIfNull(value);
                ExceptionHelper.ThrowByCondition(value.VertexShader == null, _ShaderMissException);
                _Shader = value;
            }
        }
        public Color Color { get; set; } = Color.White;
        public double Reflectivity
        {
            get => _Reflectivity;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange01(value);
                _Reflectivity = value;
            }
        }
        public double Roughness
        {
            get => _Roughness;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange01(value);
                _Roughness = value;
            }
        }
        private double _Reflectivity = 1;
        private double _Roughness = 1;
        private Shader _Shader;
        private const string _ShaderMissException = "Vertex shader is required";
    }
}
