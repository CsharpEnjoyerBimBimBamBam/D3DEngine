using SharpDX;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DirectXEngine
{
    internal static class Extenstions
    {
        public static Vector4 ToVector4(this Vector3 vector, int w = 1) => new Vector4(vector.X, vector.Y, vector.Z, w);

        public static Vector3 ToVector3(this Vector4 vector, bool devideByW = true) =>
            devideByW ? new Vector3(vector.X / vector.W, vector.Y / vector.W, vector.Z / vector.W) : new Vector3(vector.X, vector.Y, vector.Z);

        public static Vector3 Normalized(this Vector3 vector) => vector / vector.Length();

        public static void SafetyForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            int index = 0;
            while (true)
            {
                if (index >= enumerable.Count())
                    return;

                action(enumerable.ElementAt(index));
                index++;
            }
        }
    }
}
