using SharpDX;
using System;
using System.Collections.Generic;

namespace DirectXEngine
{
    public class Frustum
    {
        public Frustum(FrustumData data, SquareCorners nearClipPlaneCorners, SquareCorners farClipPlaneCorners)
        {
            Data = data;
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
        public SquareCorners FarClipPlaneCorners { get; }
        public SquareCorners NearClipPlaneCorners { get; }
        public Vector3 Center { get; }
        public FrustumData Data { get; }
        private Vector3[] _AllCorners;
        private Matrix? _ViewProjectionMatrix;

        public bool CheckForIntersection(Transform transform, Bounds bounds)
        {
            Vector3[] corners = bounds.CalculateCorners(transform);

            Matrix viewProjectionMatrix;

            if (_ViewProjectionMatrix != null)
                viewProjectionMatrix = (Matrix)_ViewProjectionMatrix;
            else
            {
                Vector3 position = Data.Position;
                Vector3 forward = Data.Forward;
                Vector3 up = Data.Up;
                Vector3 target = position + forward;

                Matrix.LookAtLH(ref position, ref target, ref up, out Matrix viewMatrix);
                Matrix.PerspectiveFovLH(Data.FieldOfView, 1, Data.NearClipPlane, Data.FarClipPlane, out Matrix projectionMatrix);
                Matrix.Multiply(ref viewMatrix, ref projectionMatrix, out viewProjectionMatrix);
                _ViewProjectionMatrix = viewProjectionMatrix;
            }

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3.Transform(ref corners[i], ref viewProjectionMatrix, out Vector3 cornerLocal);
                Vector3.Abs(ref cornerLocal, out Vector3 absCorner);

                if (absCorner.X < 1 && absCorner.Y < 1 && absCorner.Z < 1)
                    return true;
            }

            return false;
        }

        public static Frustum Calculate(Camera camera)
        {
            FrustumData data = FrustumData.Calculate(camera);

            float nearClipPlane = camera.NearClipPlane;
            float farClipPlane = camera.FarClipPlane;

            SquareCorners nearClipPlaneCorners = SquareCorners.Calculate(data, nearClipPlane);
            SquareCorners farClipPlaneCorners = SquareCorners.Calculate(data, farClipPlane);

            return new Frustum(data, nearClipPlaneCorners, farClipPlaneCorners);
        }

        public static Frustum[] CalculateSubFrustums(Camera camera, int count) => CalculateSubFrustums(camera, camera.FarClipPlane, camera.NearClipPlane, count);

        public static Frustum[] CalculateSubFrustums(Camera camera, float nearClipPlane, float farClipPlane, int count)
        {
            ExceptionHelper.ThrowIfOutOfRange(count, 1, double.PositiveInfinity);

            FrustumData data = FrustumData.Calculate(camera);

            float frustrumLength = farClipPlane - nearClipPlane;
            float subFrustrumLength = frustrumLength / count;

            Frustum[] subFrustrums = new Frustum[count];

            for (int i = 0; i < count; i++)
            {
                float currentNearClipPlane = nearClipPlane + (subFrustrumLength * i);
                float currentFarClipPlane = nearClipPlane + (subFrustrumLength * (i + 1));

                SquareCorners nearClipPlaneCorners = SquareCorners.Calculate(data, currentNearClipPlane);
                SquareCorners farClipPlaneCorners = SquareCorners.Calculate(data, currentFarClipPlane);

                FrustumData currentData = data;
                currentData.NearClipPlane = currentNearClipPlane;
                currentData.FarClipPlane = currentFarClipPlane;

                subFrustrums[i] = new Frustum(currentData, nearClipPlaneCorners, farClipPlaneCorners);
            }

            return subFrustrums;
        }

        public struct FrustumData
        {
            public Vector3 Position;
            public Vector3 Forward;
            public Vector3 Right;
            public Vector3 Up;
            public float HalfFieldOfViewTan;
            public float FieldOfView;
            public float NearClipPlane;
            public float FarClipPlane;

            public static FrustumData Calculate(Camera camera)
            {
                Transform transform = camera.Transform;

                Quaternion rotation = transform.WorldRotation;
                Vector3 position = transform.WorldPosition;
                Vector3 forward = transform.GetForward(rotation);
                Vector3 right = transform.GetRight(rotation);
                Vector3 up = transform.GetUp(rotation);
                float halfFieldOfViewTan = (float)Math.Tan(camera.FieldOfViewRadians / 2);

                return new FrustumData
                {
                    Position = position,
                    Forward = forward,
                    Right = right,
                    Up = up,
                    HalfFieldOfViewTan = halfFieldOfViewTan,
                    FieldOfView = camera.FieldOfViewRadians,
                };
            }
        }
    }
}
