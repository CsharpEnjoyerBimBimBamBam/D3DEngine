using SharpDX;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DirectXEngine
{
    internal static class EngineUtilities
    {
        public static byte[] ToByteArray<T>(ref T data, bool align = false) where T : struct 
        {
            int dataSize = Marshal.SizeOf(data);
            int alignedDataSize = dataSize;

            if (align)
                alignedDataSize = CalculateAlignedSize(dataSize);
            
            byte[] array = new byte[alignedDataSize];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(dataSize);
                Marshal.StructureToPtr(data, ptr, false);
                Marshal.Copy(ptr, array, 0, dataSize);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
            return array;
        }

        public static byte[] ToByteArray<T>(IReadOnlyList<T> data, bool align = false) where T : struct
        {
            if (data.Count == 0)
                return new byte[0];

            int size = Utilities.SizeOf<T>();
            int bytesCount = size * data.Count;

            if (align)
                bytesCount = CalculateAlignedSize(bytesCount);

            byte[] array = new byte[bytesCount];
            int index = 0;

            for (int i = 0; i < data.Count; i++)
            {
                T item = data[i];
                byte[] dataBytes = ToByteArray(ref item);

                for (int j = 0; j < dataBytes.Length; j++)
                {
                    array[index] = dataBytes[j];
                    index++;
                }
            }

            return array;
        }

        public static int GetAlignedSize<T>() where T : struct => CalculateAlignedSize(Utilities.SizeOf<T>());

        private static int CalculateAlignedSize(int size)
        {
            int remainder = size % 16;
            
            if (remainder == 0)
                return size;

            return size + (16 - remainder);            
        }
    }
}
