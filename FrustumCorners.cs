using SharpDX;
using System;
using System.Collections.Generic;

namespace DirectXEngine
{
    public class FrustumCorners
    {
        public FrustumCorners(Vector3 leftDown, Vector3 leftUp, Vector3 rightUp, Vector3 rightDown)
        {
            LeftDown = leftDown;
            LeftUp = leftUp;
            RightUp = rightUp;
            RightDown = rightDown;
            Center = (leftDown + leftUp + rightUp + rightDown) / 4;
            _All = new Vector3[]
            {
                leftDown,
                leftUp,
                rightUp,
                rightDown,
            };
        }

        public IReadOnlyList<Vector3> All => _All;
        public Vector3 LeftDown { get; }
        public Vector3 LeftUp { get; }
        public Vector3 RightUp { get; }
        public Vector3 RightDown { get; }
        public Vector3 Center { get; }
        private Vector3[] _All;

        public static FrustumCorners Calculate(Vector3 position, Vector3 forward, Vector3 right, Vector3 up, float halfFieldOfViewTan, float clipPlaneDistance)
        {
            Vector3 clipPlaneCenter = position + (forward * clipPlaneDistance);

            float halfClipPlaneSize = clipPlaneDistance * halfFieldOfViewTan;

            Vector3 rightScaled = right * halfClipPlaneSize;
            Vector3 upScaled = up * halfClipPlaneSize;

            Vector3 leftDown = clipPlaneCenter - rightScaled - upScaled;
            Vector3 leftUp = clipPlaneCenter - rightScaled + upScaled;
            Vector3 rightUp = clipPlaneCenter + rightScaled + upScaled;
            Vector3 rightDown = clipPlaneCenter + rightScaled - upScaled;

            return new FrustumCorners(leftDown, leftUp, rightUp, rightDown);
        }
    }
}
