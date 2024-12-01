using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Mathematics;

namespace DirectXEngine
{
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    internal struct ConstantBufferInput
    {
        public Matrix ModelViewProjection;
        public Matrix ModelLocalToWorldDirection;
        public Matrix ModelLocalToWorld;
        public Vector3 CameraPosition;
        public Vector3 CameraForward;
        public float GlobalIllumination;
        public int IsHaveTexture;
    }
}
