using System;
using System.Windows.Forms;
using SharpDX;
using System.Collections.Generic;
using System.Linq;

namespace DirectXEngine
{
    public class Mesh : ICloneable
    {
        public Vector3[] Vertices
        {
            get => _Vertices;
            set
            {
                ExceptionHelper.ThrowIfNull(value);
                _Vertices = value;
            }
        }
        public int[] Triangles
        {
            get => _Triangles;
            set
            {
                ExceptionHelper.ThrowIfNull(value);
                ExceptionHelper.ThrowByCondition(value.Length % 3 != 0);
                _Triangles = value;
            }
        }
        public Vector3[] Normals
        {
            get => _Normals;
            set
            {
                ExceptionHelper.ThrowIfNull(value);
                ExceptionHelper.ThrowIfOutOfRange(value.Length, _Vertices.Length, _Vertices.Length);
                _Normals = value;
            }
        }
        public Vector2[] UVs
        {
            get => _UVs;
            set 
            {
                ExceptionHelper.ThrowIfNull(value);
                ExceptionHelper.ThrowIfOutOfRange(value.Length, _Vertices.Length, _Vertices.Length);
                _UVs = value;
            }
        }
        public int TrianglesCount => Triangles.Length / 3;
        private Vector3[] _Vertices = new Vector3[0];
        private int[] _Triangles = new int[0];
        private Vector3[] _Normals = new Vector3[0];
        private Vector2[] _UVs = new Vector2[0];

        public void RecalculateNormals()
        {
            _Normals = new Vector3[_Vertices.Length];

            for (int i = 0; i < _Vertices.Length; i++)
            {
                _Normals[i] = CalculateAverageTrianglesNormal(i);
            }
        }

        private Vector3 CalculateAverageTrianglesNormal(int vertexIndex)
        {
            Vector3 normal = Vector3.Zero;
            int trianglePosition = 0;
            int count = 0;

            for (int i = 0; i < _Triangles.Length; i++)
            {
                if (_Triangles[i] == vertexIndex)
                {
                    int triangleIndex1 = _Triangles[i - trianglePosition];
                    int triangleIndex2 = _Triangles[i - (trianglePosition - 1)];
                    int triangleIndex3 = _Triangles[i - (trianglePosition - 2)];

                    Vector3 vertex1 = _Vertices[triangleIndex1];
                    Vector3 vertex2 = _Vertices[triangleIndex2];
                    Vector3 vertex3 = _Vertices[triangleIndex3];

                    Vector3 currentNormal = Vector3.Cross(vertex3 - vertex2, vertex1 - vertex2);

                    normal += currentNormal;
                    count++;
                }

                trianglePosition++;

                if (trianglePosition > 2)
                    trianglePosition = 0;
            }
            
            normal.Normalize();

            return normal;
        }

        public object Clone() => new Mesh
        {
            _Vertices = (Vector3[])_Vertices.Clone(),
            _Triangles = (int[])_Triangles.Clone(),
            _Normals = (Vector3[])_Normals.Clone(),
        };
    }
}
