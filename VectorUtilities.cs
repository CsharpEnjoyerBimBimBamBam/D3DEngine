using System;
using SharpDX;

namespace DirectXEngine
{
    public static class VectorUtilities
    {
        public static float Angle(Vector3 vector1, Vector3 vector2) => (float)Math.Acos(CosAngle(vector1, vector2));

        public static float CosAngle(Vector3 vector1, Vector3 vector2)
        {
            float lengthProduct = vector1.Length() * vector2.Length();

            if (lengthProduct == 0)
                return 1;

            Vector3.Dot(ref vector1, ref vector2, out float dotProduct);

            return dotProduct / lengthProduct;
        }

        public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 normalToPlane)
        {
            Vector3.Cross(ref normalToPlane, ref vector, out Vector3 crossProduct);
            Vector3.Cross(ref crossProduct, ref normalToPlane, out Vector3 project);
            return project;
        }

        public static Vector3 ToEulerAngles(Quaternion rotation)
        {
            float x = rotation.X;
            float y = rotation.Y;
            float z = rotation.Z;
            float w = rotation.W;

            float xSquared = x * x;
            float ySquared = y * y;
            float zSquared = z * z;

            float rootValue = 2 * ((w * y) - (x * z));
            rootValue = MathUtil.Clamp(rootValue, -1, 1);
            float rotationX = (float)Math.Asin(rootValue);

            float sinY = 2 * ((w * z) + (x * y));
            float cosY = 1 - (2 * (ySquared + zSquared));
            float rotationY = (float)Math.Atan2(sinY, cosY);

            float sinZ = 2 * ((w * x) + (y * z));
            float cosZ = 1 - 2 * (xSquared + ySquared);
            float rotationZ = (float)Math.Atan2(sinZ, cosZ);

            return new Vector3(rotationX, rotationY, rotationZ);
        }
    }
}
