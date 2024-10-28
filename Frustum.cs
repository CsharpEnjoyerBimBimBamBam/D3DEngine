using SharpDX;
using System;
using System.Collections.Generic;

namespace DirectXEngine
{
    public class Frustum
    {
        public Frustum(FrustumCorners nearClipPlaneCorners, FrustumCorners farClipPlaneCorners)
        {
            NearClipPlaneCorners = nearClipPlaneCorners;
            FarClipPlaneCorners = farClipPlaneCorners;
            Center = (nearClipPlaneCorners.Center + farClipPlaneCorners.Center) / 2;
            _AllCorners = new Vector3[]
            {
                nearClipPlaneCorners.LeftDown,
                nearClipPlaneCorners.LeftUp,
                nearClipPlaneCorners.RightUp,
                nearClipPlaneCorners.RightDown,
                farClipPlaneCorners.LeftDown,
                farClipPlaneCorners.LeftUp,
                farClipPlaneCorners.RightUp,
                farClipPlaneCorners.RightDown,
            };
        }

        public IReadOnlyList<Vector3> AllCorners => _AllCorners;
        public FrustumCorners FarClipPlaneCorners { get; }
        public FrustumCorners NearClipPlaneCorners { get; }
        public Vector3 Center { get; }
        private Vector3[] _AllCorners;

        public static Frustum Calculate(Camera camera)
        {
            FrustumData data = FrustumData.Calculate(camera);

            Vector3 position = data.Position;
            Vector3 forward = data.Forward;
            Vector3 right = data.Right;
            Vector3 up = data.Up;
            float halfFieldOfViewTan = data.HalfFieldOfViewTan;
            float nearClipPlane = camera.NearClipPlane;
            float farClipPlane = camera.FarClipPlane;

            FrustumCorners nearClipPlaneCorners = FrustumCorners.Calculate(position, forward, right, up, halfFieldOfViewTan, nearClipPlane);
            FrustumCorners farClipPlaneCorners = FrustumCorners.Calculate(position, forward, right, up, halfFieldOfViewTan, farClipPlane);

            return new Frustum(nearClipPlaneCorners, farClipPlaneCorners);
        }

        public static Frustum[] CalculateSubFrustums(Camera camera, int count)
        {
            ExceptionHelper.ThrowIfOutOfRange(count, 1, double.PositiveInfinity);

            FrustumData data = FrustumData.Calculate(camera);

            Vector3 position = data.Position;
            Vector3 forward = data.Forward;
            Vector3 right = data.Right;
            Vector3 up = data.Up;
            float halfFieldOfViewTan = data.HalfFieldOfViewTan;

            float nearClipPlane = camera.NearClipPlane;
            float farClipPlane = camera.FarClipPlane;
            float frustrumLength = farClipPlane - nearClipPlane;
            float subFrustrumLength = frustrumLength / count;

            Frustum[] subFrustrums = new Frustum[count];

            for (int i = 0; i < count; i++)
            {
                float currentNearClipPlane = nearClipPlane + (subFrustrumLength * i);
                float currentFarClipPlane = nearClipPlane + (subFrustrumLength * (i + 1));

                FrustumCorners nearClipPlaneCorners = FrustumCorners.Calculate(position, forward, right, up, halfFieldOfViewTan, currentNearClipPlane);
                FrustumCorners farClipPlaneCorners = FrustumCorners.Calculate(position, forward, right, up, halfFieldOfViewTan, currentFarClipPlane);

                subFrustrums[i] = new Frustum(nearClipPlaneCorners, farClipPlaneCorners);
            }

            return subFrustrums;
        }

        private class FrustumData
        {
            public Vector3 Position;
            public Vector3 Forward;
            public Vector3 Right;
            public Vector3 Up;
            public float HalfFieldOfViewTan;

            public static FrustumData Calculate(Camera camera)
            {
                Transform transform = camera.Transform;

                Matrix rotationMatrix = transform.RotationMatrix;
                Vector3 position = transform.WorldPosition;
                Vector3 forward = transform.GetForward(rotationMatrix);
                Vector3 right = transform.GetRight(rotationMatrix);
                Vector3 up = transform.GetUp(rotationMatrix);
                float halfFieldOfViewTan = (float)Math.Tan(camera.FieldOfViewRadians / 2);

                return new FrustumData
                {
                    Position = position,
                    Forward = forward,
                    Right = right,
                    Up = up,
                    HalfFieldOfViewTan = halfFieldOfViewTan
                };
            }
        }
    }
}
