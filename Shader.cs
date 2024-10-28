using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;

namespace DirectXEngine
{
    public class Shader
    {
        public Shader(InputElement[] vertexShaderInput, PrimitiveTopology topology, int constantBufferSize)
        {
            ExceptionHelper.ThrowIfNull(vertexShaderInput);
            ExceptionHelper.ThrowIfOutOfRange(constantBufferSize, 0, double.PositiveInfinity);
            ExceptionHelper.ThrowByCondition(constantBufferSize % 16 != 0);
            VertexShaderInput = vertexShaderInput;
            Topology = topology;
            ConstantBufferSize = constantBufferSize;
        }
        
        public InputElement[] VertexShaderInput { get; private set; }
        public PrimitiveTopology Topology { get; private set; }
        public int ConstantBufferSize { get; private set; }
        public ShaderData VertexShader 
        {
            get => _VertexShader;
            set
            {
                CheckShader(value);
                _VertexShader = value;
            }
        }
        public ShaderData PixelShader
        {
            get => _PixelShader;
            set
            {
                CheckShader(value);
                _PixelShader = value;
            }
        }
        public static Shader Default
        {
            get
            {
                InputElement[] inputElements = new InputElement[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0),
                };

                int constantBufferSize = Utilities.SizeOf<ConstantBufferInput>();

                Shader meshShader = new Shader(inputElements, PrimitiveTopology.PointList, constantBufferSize);

                ShaderData vertexShader = new ShaderData(ShaderProfile.vs_4_0, "VS");
                ShaderData pixelShader = new ShaderData(ShaderProfile.ps_5_0, "PS");

                const string source = 
                    "float4 VS(float4 input)" +
                    "{" +
                        "return float4(0, 0, 0, 1);" +
                    "}" +
                        "float3 PS(float4 input) : SV_Target" +
                    "{" +
                        "return float3(1, 1, 1);" +
                    "}";

                vertexShader.CompileFromSourceCode(source);
                pixelShader.CompileFromSourceCode(source);

                meshShader.VertexShader = vertexShader;
                meshShader.PixelShader = pixelShader;

                return meshShader;
            }
        }
        private ShaderData _VertexShader;
        private ShaderData _PixelShader;
        private const string _ShaderNotCompiledException = "Shader not compiled";

        private void CheckShader(ShaderData data)
        {
            ExceptionHelper.ThrowIfNull(data);
            ExceptionHelper.ThrowByCondition(data, _ShaderNotCompiledException, e => !e.IsCompiled);
        }
    }
}
