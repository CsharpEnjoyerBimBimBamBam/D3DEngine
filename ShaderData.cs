using SharpDX.D3DCompiler;
using System.Text.Json.Serialization;

namespace DirectXEngine
{
    public class ShaderData
    {
        public ShaderData(ShaderProfile profile, string mainFunctionName)
        {
            _MainFunctionName = mainFunctionName;
            _Profile = profile;
        }

        public ShaderData(CompilationResult byteCode)
        {
            CheckByteCode(byteCode);
        }

        public void CompileFromFile(string filePath)
        {
            CompilationResult byteCode = ShaderBytecode.CompileFromFile(filePath, _MainFunctionName, _Profile.ToString());
            CheckByteCode(byteCode);
        }

        public void CompileFromSourceCode(string sourceCode)
        {
            CompilationResult byteCode = ShaderBytecode.Compile(sourceCode, _MainFunctionName, _Profile.ToString());
            CheckByteCode(byteCode);
        }

        public CompilationResult ByteCode { get; private set; }
        public ShaderSignature Signature { get; private set; }
        public bool IsCompiled { get; private set; } = false;
        private string _MainFunctionName;
        private ShaderProfile _Profile;
        private const string _ShaderCompileException = "Can not compile shader";

        private void CheckByteCode(CompilationResult byteCode)
        {
            ExceptionHelper.ThrowByCondition(byteCode == null, _ShaderCompileException);
            Signature = new ShaderSignature(byteCode);
            ByteCode = byteCode;
            IsCompiled = true;
        }
    }
}
