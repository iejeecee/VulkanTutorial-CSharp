using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    unsafe class VkRenderPass : IDisposable
    {
        RenderPass renderPass;
        readonly Vk vk;
        readonly VkDevice device;
        private bool disposedValue;

        public VkRenderPass(VkDevice device, in RenderPassCreateInfo createInfo)
        {
            vk = Vk.GetApi();
            this.device = device;

            Result result = vk.CreateRenderPass(device, createInfo, null, out renderPass);

            if (result != Result.Success)
            {
                ResultException.Throw(result, "Error creating render pass");
            }
        }

        public static implicit operator RenderPass(VkRenderPass r) => r.renderPass;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                vk.DestroyRenderPass(device, this, null);

                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~VkRenderPass()
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

