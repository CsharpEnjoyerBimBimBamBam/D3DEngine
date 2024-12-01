using SharpDX;
using System;
using System.Collections.Generic;

namespace DirectXEngine
{
    public class SquareCorners
    {
        public SquareCorners(Vector3 leftDown, Vector3 leftUp, Vector3 rightUp, Vector3 rightDown)
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

        public static SquareCorners Calculate(Frustum.FrustumData data, float clipPlaneDistance)
        {
            Vector3 clipPlaneCenter = data.Position + (data.Forward * clipPlaneDistance);

            float halfClipPlaneSize = clipPlaneDistance * data.HalfFieldOfViewTan;

            Vector3 rightScaled = data.Right * halfClipPlaneSize;
            Vector3 upScaled = data.Up * halfClipPlaneSize;

            Vector3 leftDown = clipPlaneCenter - rightScaled - upScaled;
            Vector3 leftUp = clipPlaneCenter - rightScaled + upScaled;
            Vector3 rightUp = clipPlaneCenter + rightScaled + upScaled;
            Vector3 rightDown = clipPlaneCenter + rightScaled - upScaled;

            return new SquareCorners(leftDown, leftUp, rightUp, rightDown);
        }
    }
}
