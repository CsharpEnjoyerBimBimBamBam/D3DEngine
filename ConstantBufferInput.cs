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
        public Matrix CameraWorldToLocal;
        public Matrix CameraLocalToScreen;
        public Matrix CameraScreenToWorld;
        public Vector4 CameraPosition;
        public float GlobalIllumination;
        private Vector3 _Padding1;
    }
}
