using System;
using SharpDX;

namespace DirectXEngine
{
    public class Material
    {
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
                ExceptionHelper.ThrowByCondition(value.VertexShader == null && value.PixelShader == null, _ShaderMissException);
                _Shader = value;
            }
        }
        public Color Color { get; set; } = Color.White;
        public Texture Texture
        {
            get => _Texture;
            set
            {
                _Texture = value;
                TextureChanged?.Invoke(_Texture);
            }
        }
        internal event Action<Texture> TextureChanged;
        private Texture _Texture;
        private Shader _Shader;
        private const string _ShaderMissException = "Material must have at least vertex shader or pixel shader";
    }

    public enum ShaderDataType
    {
        Matrix,
        Vector3,
        Vector4,
        Float,
        Int
    }
}
