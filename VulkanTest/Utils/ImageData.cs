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
    unsafe class ImageData
    {
        Format format;

        public ImageData(
            VkPhysicalDevice physicalDevice,
            VkDevice device,
            Format format,
            Extent2D extent,
            ImageTiling tiling,
            ImageUsageFlags usage,
            ImageLayout initialLayout,
            MemoryPropertyFlags memoryProperties,
            ImageAspectFlags aspectMask)     
        {
            this.format = format;

            ImageCreateInfo imageCreateInfo = new()                
            {
                SType = StructureType.ImageCreateInfo,
                ImageType = ImageType.ImageType2D,
                MipLevels = 1,
                ArrayLayers = 1,
                Format = format,
                Tiling = tiling,
                InitialLayout = initialLayout,
                Usage = usage | ImageUsageFlags.ImageUsageSampledBit,
                SharingMode = SharingMode.Exclusive,
                Samples = SampleCountFlags.SampleCount1Bit
            };

            imageCreateInfo.Extent.Width = extent.Width;
            imageCreateInfo.Extent.Height = extent.Height;
            imageCreateInfo.Extent.Depth = 1;

            /*deviceMemory = vk::su::allocateDeviceMemory(device, physicalDevice.GetMemoryProperties(), device.GetImageMemoryRequirements(image), memoryProperties);

                device.BindImageMemory(image, deviceMemory, 0);

                vk::ImageViewCreateInfo imageViewCreateInfo( { }, image, vk::ImageViewType::e2D, format, { }, { aspectMask, 0, 1, 0, 1 } );
                imageView = device.createImageView(imageViewCreateInfo);*/
            }
        }
    
}
