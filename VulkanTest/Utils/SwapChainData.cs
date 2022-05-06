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
        ImageView[] imageViews;

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
                Extent2D actualExtent = new()
                {
                    Width = surfaceData.Extent.Width,
                    Height = surfaceData.Extent.Height
                };

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

            SwapchainCreateInfoKHR createInfo = new()
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                Surface = surfaceData.Surface,
                MinImageCount = surfaceCapabilities.MinImageCount,
                ImageFormat = colorFormat,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageExtent = swapchainExtent,
                ImageArrayLayers = 1,
                ImageUsage = usage,
                ImageSharingMode = SharingMode.Exclusive,
                PreTransform = preTransform,
                CompositeAlpha = compositeAlpha,
                PresentMode = presentMode,
                OldSwapchain = oldSwapChain,
                Clipped = true,
            };

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

            swapChain = device.CreateSwapchainKHR(createInfo);
            images = device.GetSwapchainImagesKHR(swapChain);

            imageViews = new ImageView[images.Length];

            for (int i = 0; i < images.Length; i++)
            {
                ImageViewCreateInfo viewInfo = new()
                {
                    SType = StructureType.ImageViewCreateInfo,
                    Image = images[i],
                    ViewType = ImageViewType.ImageViewType2D,
                    Format = colorFormat
                };
                viewInfo.SubresourceRange.AspectMask = ImageAspectFlags.ImageAspectColorBit;
                viewInfo.SubresourceRange.BaseMipLevel = 0;
                viewInfo.SubresourceRange.LevelCount = 1;
                viewInfo.SubresourceRange.BaseArrayLayer = 0;
                viewInfo.SubresourceRange.LayerCount = 1;

                imageViews[i] = device.CreateImageView(viewInfo);
            }
           
        }

        public void Clear(VkDevice device)
        {
            foreach (var imageView in imageViews)
            {
                device.DestroyImageView(imageView);
            }

            device.DestroySwapchainKHR(swapChain);
        }

        public ImageView[] ImageViews { get => imageViews; protected set => imageViews = value; }
        public Image[] Images { get => images; protected set => images = value; }
        public VkSwapChainKHR SwapChain { get => swapChain; protected set => swapChain = value; }
        public Format ColorFormat { get => colorFormat; protected set => colorFormat = value; }
        public Extent2D SwapchainExtent { get => swapchainExtent; protected set => swapchainExtent = value; }
    }
}
