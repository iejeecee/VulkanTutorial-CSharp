using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using VulkanTest.Exceptions;
using VulkanTest.Utils;

namespace VulkanTest.VulkanObject
{
    unsafe class VkDeviceMemory : IDisposable
    {
        readonly Vk vk;
        readonly VkDevice device;

        DeviceMemory memory;
        private bool disposedValue;

        public VkDeviceMemory(VkDevice device, in MemoryAllocateInfo allocateInfo)
        {
            vk = Vk.GetApi();
            this.device = device;
           
            Result result = vk.AllocateMemory(device, allocateInfo, null, out memory);

            if (result != Result.Success)
            {
                ResultException.Throw(result, "Error allocating device memory");
            }

        }

        public void* MapMemory(ulong offset, ulong size, uint flags = 0)
        {
            void* data;

            Result result = vk.MapMemory(device, this, offset, size, flags, &data);

            if (result != Result.Success)
            {
                ResultException.Throw(result, "Error mapping device memory");
            }

            return data;
        }

        public void UnmapMemory()
        {
            vk.UnmapMemory(device, this);           
        }

        public static implicit operator DeviceMemory(VkDeviceMemory m) => m.memory;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                vk.FreeMemory(device, memory, null);
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~VkDeviceMemory()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
