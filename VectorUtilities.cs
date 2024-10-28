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
    }
}
