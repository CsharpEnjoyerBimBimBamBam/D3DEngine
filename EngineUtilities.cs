using SharpDX;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DirectXEngine
{
    internal static class EngineUtilities
    {
        public static byte[] ToByteArray<T>(ref T data) where T : struct 
        {
            int size = Utilities.SizeOf<T>();
            byte[] array = new byte[size];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(data, ptr, false);
                Marshal.Copy(ptr, array, 0, size);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
            return array;
        }

        public static byte[] ToByteArray<T>(T[] data) where T : struct
        {
            if (data.Length == 0)
                return new byte[0];

            int size = Utilities.SizeOf<T>();

            byte[] array = new byte[size * data.Length];
            int index = 0;

            for (int i = 0; i < data.Length; i++)
            {
                byte[] dataBytes = ToByteArray(ref data[i]);

                for (int j = 0; j < dataBytes.Length; j++)
                {
                    array[index] = dataBytes[j];
                    index++;
                }
            }

            return array;
        }
    }
}
