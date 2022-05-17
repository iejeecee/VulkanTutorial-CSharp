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
    unsafe class VkBuffer : IDisposable
    {
        readonly Vk vk;
        readonly VkDevice device;

        Silk.NET.Vulkan.Buffer buffer;
        private bool disposedValue;

        public VkBuffer(VkDevice device, in BufferCreateInfo info)
        {
            vk = Vk.GetApi();
            this.device = device;

            Result result = vk.CreateBuffer(device, in info, null, out buffer);

            if (result != Result.Success)
            {
                ResultException.Throw(result, "Error creating buffer");
            }

        }

        public void BindMemory(VkDeviceMemory memory, ulong memoryOffset = 0)
        {
            vk.BindBufferMemory(device, this, memory, memoryOffset);
        }

        public MemoryRequirements GetMemoryRequirements()
        {
            vk.GetBufferMemoryRequirements(device, this, out MemoryRequirements memoryRequirements);

            return memoryRequirements;
        }

        public static implicit operator Silk.NET.Vulkan.Buffer(VkBuffer b) => b.buffer;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                vk.DestroyBuffer(device, buffer, null);
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~VkBuffer()
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

