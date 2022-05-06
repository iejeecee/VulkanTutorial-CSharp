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

        public VkCommandPool CreateCommandPool(in CommandPoolCreateInfo info)
        {
            Result result = vk.CreateCommandPool(device, in info, null, out CommandPool commandPool);

            if (result != Result.Success)
            {
                throw new ResultException(nameof(CreateCommandPool));
            }

            return new VkCommandPool(commandPool);
        }

        public void DestroyCommandPool(VkCommandPool commandPool)
        {
            vk.DestroyCommandPool(device, commandPool, null);
        }

        public VkSwapChainKHR CreateSwapchainKHR(in SwapchainCreateInfoKHR info)
        {
            VkSwapChainKHR swapChain = new(instance, this, info);

            return swapChain;
        }

        public void DestroySwapchainKHR(VkSwapChainKHR swapChain)
        {
            swapChain.Clear();
        }

        public (uint imageIndex, Result result) AquireNextImage(VkSwapChainKHR swapChain, ulong timeout, in Semaphore semaphore, in Fence fence)
        {
            KhrSwapchain khrSwapchain = swapChain.KhrSwapchain;

            uint imageIndex = 0;

            var result = khrSwapchain.AcquireNextImage(this, swapChain, timeout, semaphore, fence, ref imageIndex);
           
            return (imageIndex, result);
        }

        public ImageView CreateImageView(ImageViewCreateInfo createInfo)
        {
            Result result = vk.CreateImageView(device, in createInfo, null, out ImageView imageView);

            if (result != Result.Success)
            {
                throw new ResultException(nameof(CreateImageView));
            }

            return imageView;
        }

        public void DestroyImageView(ImageView imageView)
        {
            vk.DestroyImageView(device, imageView, null);         
        }

        public Image[] GetSwapchainImagesKHR(VkSwapChainKHR swapchain)
        {
            KhrSwapchain khrSwapchain = swapchain.KhrSwapchain;
          
            uint imageCount;

            khrSwapchain.GetSwapchainImages(device, swapchain, &imageCount, null);
            Image[] swapChainImages = new Image[imageCount];
            khrSwapchain.GetSwapchainImages(device, swapchain, &imageCount, swapChainImages);

            return swapChainImages;
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
