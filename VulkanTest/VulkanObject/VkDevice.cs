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
    unsafe class VkDevice : IDisposable
    {
        readonly VkInstance instance;     
        readonly Vk vk;
        Device device;
        private bool disposedValue;

        public VkInstance Instance => instance;

        public VkDevice(VkInstance instance, Device device)           
        {          
            vk = Vk.GetApi();
          
            this.device = device;
            this.instance = instance;
        }

        public void WaitIdle()
        {
            vk.DeviceWaitIdle(device);
        }

        public VkQueue GetQueue(uint queueFamilyIndex, uint queueIndex = 0)
        {
            vk.GetDeviceQueue(device, queueFamilyIndex, queueIndex, out Queue queue);

            return new VkQueue(queue);
        }

        public void WaitForFences(VkFence fence, ulong timeout)
        {
            var result = vk.WaitForFences(device, 1, fence, true, timeout);

            if (result != Result.Success)
            {
                ResultException.Throw(result, "Error waiting for fence");
            }
        }

        public void WaitForFences(ReadOnlySpan<Fence> fences, bool waitAll, ulong timeout)
        {
            var result = vk.WaitForFences(device, fences, waitAll, timeout);

            if (result != Result.Success)
            {
                ResultException.Throw(result, "Error waiting for fences");
            }
        }

        public void ResetFences(VkFence fence)
        {
            var result = vk.ResetFences(device, 1, fence);

            if (result != Result.Success)
            {
                ResultException.Throw(result, "Error resetting fence");
            }
        }

        public void ResetFences(ReadOnlySpan<Fence> fences)
        {
            var result = vk.ResetFences(device, fences);

            if (result != Result.Success)
            {
                ResultException.Throw(result, "Error resetting fences");
            }
        }

        public void UpdateDescriptorSets(ReadOnlySpan<WriteDescriptorSet> descriptorWrites, ReadOnlySpan<CopyDescriptorSet> descriptorCopies)
        {
            vk.UpdateDescriptorSets(device, descriptorWrites, descriptorCopies);
        }

        public static implicit operator Device(VkDevice d) => d.device;
             
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                vk.DestroyDevice(device, null);

                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~VkDevice()
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
