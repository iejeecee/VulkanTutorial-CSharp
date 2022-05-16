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
using VulkanTest.VulkanObject;


namespace VulkanTest.Utils
{
    unsafe class BufferData : IDisposable
    {
        public VkBuffer buffer;
        public VkDeviceMemory deviceMemory;
        readonly ulong size;
        readonly BufferUsageFlags usage;
        readonly MemoryPropertyFlags propertyFlags;
        private bool disposedValue;
       
        public BufferData(VkPhysicalDevice physicalDevice,
                    VkDevice device,
                    ulong size,
                    BufferUsageFlags usage,
                    MemoryPropertyFlags propertyFlags = MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit)
        {
            this.size = size;
            this.usage = usage;
            this.propertyFlags = propertyFlags;

            buffer = new VkBuffer(device, new(size: size, usage: usage));

            deviceMemory = SU.AllocateDeviceMemory(device,
                physicalDevice.GetMemoryProperties(),
                buffer.GetMemoryRequirements(),
                propertyFlags);

            buffer.BindMemory(deviceMemory);
        }


        public void Upload<T>(T data) where T : struct
        {
            uint dataSize = (uint)Marshal.SizeOf(data);

            Debug.Assert(propertyFlags.HasFlag(MemoryPropertyFlags.MemoryPropertyHostCoherentBit | MemoryPropertyFlags.MemoryPropertyHostVisibleBit));
            Debug.Assert(dataSize <= size);

            void* dataPtr = Unsafe.AsPointer(ref data);

            void* dstPtr = deviceMemory.MapMemory(0, dataSize);

            System.Buffer.MemoryCopy(dataPtr, dstPtr, size, dataSize);

            deviceMemory.UnmapMemory();
        }


        public void Upload<T>(Span<T> data, uint stride = 0) where T : struct
        {
            SU.CopyToDevice(deviceMemory, data, stride);
        }


        public void Upload<T>(VkPhysicalDevice physicalDevice,
                     VkDevice device,
                     VkCommandPool commandPool,
                     VkQueue queue,
                     Span<T> data,
                     uint stride = 0) where T : struct
        {
            Debug.Assert(usage.HasFlag(BufferUsageFlags.BufferUsageTransferDstBit));
            Debug.Assert(propertyFlags.HasFlag(MemoryPropertyFlags.MemoryPropertyDeviceLocalBit));

            Debug.Assert(!data.IsEmpty);

            uint elemSize = (uint)Marshal.SizeOf(data[0]);

            stride = stride > 0 ? stride : elemSize;
            Debug.Assert(elemSize <= stride);

            ulong dataSize = (uint)data.Length * stride;

            BufferData stagingBuffer = new(physicalDevice, device, dataSize, 
                BufferUsageFlags.BufferUsageTransferSrcBit);

            SU.CopyToDevice(stagingBuffer.deviceMemory, data, stride);

            SU.OneTimeSubmit(device,
                             commandPool,
                             queue,
                             commandBuffer => commandBuffer.CopyBuffer(stagingBuffer.buffer, buffer, new BufferCopy(0, 0, dataSize)));

            stagingBuffer.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                buffer.Dispose();
                deviceMemory.Dispose();

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~BufferData()
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
