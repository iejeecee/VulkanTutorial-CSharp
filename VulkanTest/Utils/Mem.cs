using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VulkanTest.Utils
{
    unsafe class Mem
    {
        public static void* AllocArray<T>(int count)
        {
            return Marshal.AllocHGlobal(Marshal.SizeOf<T>() * count).ToPointer();
        }

        public static void FreeArray(void* data)
        {
            if (data != null)
            {
                Marshal.FreeHGlobal((nint)data);
            }
        }
    }
}
