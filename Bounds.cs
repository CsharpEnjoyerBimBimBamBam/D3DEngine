using SharpDX;

namespace DirectXEngine
{
    public class Bounds
    {
        public Bounds(Vector3 center, Vector3 size) 
        { 
            Center = center;
            Size = size;
        }

        public Vector3 Size { get; }
        public Vector3 Center { get; }
        private const int _CubeVerticesCount = 8;

        public Vector3[] CalculateCorners(Transform transform)
        {
            Vector3[] corners = new Vector3[_CubeVerticesCount];

            float x = Size.X;
            float y = Size.Y;
            float z = Size.Z;

            corners[0] = Center + new Vector3(x, -y, -z);
            corners[1] = Center + new Vector3(-x, -y, -z);
            corners[2] = Center + new Vector3(-x, y, -z);
            corners[3] = Center + new Vector3(x, y, -z);

            corners[4] = Center + new Vector3(x, -y, z);
            corners[5] = Center + new Vector3(-x, -y, z);
            corners[6] = Center + new Vector3(-x, y, z);
            corners[7] = Center + new Vector3(x, y, z);

            Matrix localToWorldMatrix = transform.LocalToWorldMatrix;

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3.Transform(ref corners[i], ref localToWorldMatrix, out corners[i]);
            }

            return corners;
        }
    }
}
