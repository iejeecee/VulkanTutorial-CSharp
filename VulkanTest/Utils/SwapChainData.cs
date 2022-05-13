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
using VulkanTest.VulkanObject;

namespace VulkanTest.Utils
{
    unsafe class SwapChainData
    {
        Format colorFormat;
        Extent2D swapchainExtent;
        VkSwapChainKHR swapChain;
        Image[] images;
        VkImageView[] imageViews;

        public SwapChainData(
            VkPhysicalDevice physicalDevice,
            VkDevice device,
            SurfaceData surfaceData,
            ImageUsageFlags usage,
            SwapchainKHR oldSwapChain,
            uint graphicsQueueFamilyIndex,
            uint presentQueueFamilyIndex
            )
        {
            SurfaceFormatKHR surfaceFormat = SU.PickSurfaceFormat(physicalDevice.GetSurfaceFormatsKHR(surfaceData.Surface));
            colorFormat = surfaceFormat.Format;

            SurfaceCapabilitiesKHR surfaceCapabilities = physicalDevice.GetSurfaceCapabilitiesKHR(surfaceData.Surface);

            swapchainExtent = new();

            if (surfaceCapabilities.CurrentExtent.Width != uint.MaxValue)
            {
                swapchainExtent = surfaceCapabilities.CurrentExtent;
            }
            else
            {
                Extent2D actualExtent = new(surfaceData.Extent.Width, surfaceData.Extent.Height);
              
                swapchainExtent.Width = Math.Clamp(actualExtent.Width, surfaceCapabilities.MinImageExtent.Width, surfaceCapabilities.MaxImageExtent.Width);
                swapchainExtent.Height = Math.Clamp(actualExtent.Height, surfaceCapabilities.MinImageExtent.Height, surfaceCapabilities.MaxImageExtent.Height);
            
            }

            SurfaceTransformFlagsKHR preTransform = surfaceCapabilities.SupportedTransforms.HasFlag(SurfaceTransformFlagsKHR.SurfaceTransformIdentityBitKhr) 
                                                     ? SurfaceTransformFlagsKHR.SurfaceTransformIdentityBitKhr
                                                     : surfaceCapabilities.CurrentTransform;

            CompositeAlphaFlagsKHR compositeAlpha =
                surfaceCapabilities.SupportedCompositeAlpha.HasFlag(CompositeAlphaFlagsKHR.CompositeAlphaPreMultipliedBitKhr) ? CompositeAlphaFlagsKHR.CompositeAlphaPreMultipliedBitKhr :
                surfaceCapabilities.SupportedCompositeAlpha.HasFlag(CompositeAlphaFlagsKHR.CompositeAlphaPostMultipliedBitKhr) ? CompositeAlphaFlagsKHR.CompositeAlphaPostMultipliedBitKhr :
                surfaceCapabilities.SupportedCompositeAlpha.HasFlag(CompositeAlphaFlagsKHR.CompositeAlphaInheritBitKhr) ? CompositeAlphaFlagsKHR.CompositeAlphaInheritBitKhr :
                CompositeAlphaFlagsKHR.CompositeAlphaOpaqueBitKhr;


            PresentModeKHR presentMode = SU.PickPresentMode(physicalDevice.GetSurfacePresentModesKHR(surfaceData.Surface));

            SwapchainCreateInfoKHR createInfo = new(                            
                surface: surfaceData.Surface,
                minImageCount: surfaceCapabilities.MinImageCount,
                imageFormat: colorFormat,
                imageColorSpace: surfaceFormat.ColorSpace,
                imageExtent: swapchainExtent,
                imageArrayLayers: 1,
                imageUsage: usage,
                imageSharingMode: SharingMode.Exclusive,
                preTransform: preTransform,
                compositeAlpha: compositeAlpha,
                presentMode: presentMode,
                oldSwapchain: oldSwapChain,
                clipped: true
            );

            if (graphicsQueueFamilyIndex != presentQueueFamilyIndex)
            {
                uint *queueFamilyIndices = stackalloc uint[]{ graphicsQueueFamilyIndex, presentQueueFamilyIndex };
                // If the graphics and present queues are from different queue families, we either have to explicitly transfer
                // ownership of images between the queues, or we have to create the swapchain with imageSharingMode as
                // vk::SharingMode::eConcurrent
                createInfo.ImageSharingMode = SharingMode.Concurrent;
                createInfo.QueueFamilyIndexCount = 2;
                createInfo.PQueueFamilyIndices = queueFamilyIndices;
            }

            swapChain = new VkSwapChainKHR(device, createInfo);
            images = swapChain.GetImagesKHR();

            imageViews = new VkImageView[images.Length];

            for (int i = 0; i < images.Length; i++)
            {
                ImageViewCreateInfo viewInfo = new(                                 
                    image: images[i],
                    viewType: ImageViewType.ImageViewType2D,
                    format: colorFormat,
                    subresourceRange: new(ImageAspectFlags.ImageAspectColorBit, 0, 1, 0, 1)
                );
            
                imageViews[i] = new VkImageView(device, viewInfo);

                /*DebugUtilsObjectNameInfoEXT info = new(
                    objectType: ObjectType.ImageView,
                    objectHandle: ((ImageView)imageViews[i]).Handle,
                    pObjectName: (byte*)Marshal.StringToHGlobalAnsi($"Swapchain Image View {i}"));

                VulkanTutorial.debugUtils.SetDebugUtilsObjectName(device, info);

                info = new(
                    objectType: ObjectType.Image,
                    objectHandle: ((Image)images[i]).Handle,
                    pObjectName: (byte*)Marshal.StringToHGlobalAnsi($"Swapchain Image {i}"));

                VulkanTutorial.debugUtils.SetDebugUtilsObjectName(device, info);*/
            }
           
        }

        public void Clear(VkDevice device)
        {            
            foreach (var imageView in imageViews)
            {
                imageView.Dispose();
            }

            swapChain.Dispose();
        }

        public VkImageView[] ImageViews { get => imageViews; protected set => imageViews = value; }
        public Image[] Images { get => images; protected set => images = value; }
        public VkSwapChainKHR SwapChain { get => swapChain; protected set => swapChain = value; }
        public Format ColorFormat { get => colorFormat; protected set => colorFormat = value; }
        public Extent2D SwapchainExtent { get => swapchainExtent; protected set => swapchainExtent = value; }
    }
}
