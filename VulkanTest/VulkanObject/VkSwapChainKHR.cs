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
    unsafe class VkSwapChainKHR : IDisposable
    {             
        KhrSwapchain khrSwapchain;
        SwapchainKHR swapChain;
        VkInstance instance;
        VkDevice device;

        private bool disposedValue;
        
        public VkSwapChainKHR(VkInstance instance, VkDevice device, in SwapchainCreateInfoKHR createInfo)
        {
            Vk vk = Vk.GetApi();
            this.instance = instance;
            this.device = device;

            if (!vk.TryGetDeviceExtension(instance, device, out khrSwapchain))
            {
                throw new NotSupportedException("KHR_swapchain extension not found.");
            }

            if (khrSwapchain.CreateSwapchain(device, createInfo, null, out swapChain) != Result.Success)
            {
                throw new Exception("failed to create swap chain!");
            }

        }

        public void Clear()
        {
            khrSwapchain.DestroySwapchain(device, swapChain, null);
        }

        public static implicit operator SwapchainKHR(VkSwapChainKHR s) => s.swapChain;
        public KhrSwapchain KhrSwapchain { get => khrSwapchain; protected set => khrSwapchain = value; }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                Clear();
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~VkSwapChainKHR()
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
