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
    unsafe class ImageData : IDisposable
    {
        public Format format;
        public VkImage image;
        public VkDeviceMemory deviceMemory;
        public VkImageView imageView;
        private bool disposedValue;

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

            ImageCreateInfo imageCreateInfo = new(             
                imageType: ImageType.ImageType2D,
                mipLevels: 1,
                arrayLayers: 1,
                format: format,
                tiling: tiling,
                initialLayout: initialLayout,
                usage: usage | ImageUsageFlags.ImageUsageSampledBit,
                sharingMode: SharingMode.Exclusive,
                samples: SampleCountFlags.SampleCount1Bit,
                extent: new(extent.Width, extent.Height, 1)
            );
           
            image = new VkImage(device, imageCreateInfo);

            deviceMemory = SU.AllocateDeviceMemory(device, physicalDevice.GetMemoryProperties(),
                image.GetMemoryRequirements(), memoryProperties);

            image.BindMemory(deviceMemory);

            ImageViewCreateInfo viewInfo = new(
                image: image,
                viewType: ImageViewType.ImageViewType2D,
                format: format,
                subresourceRange: new(aspectMask, 0, 1, 0, 1)
            );
        
            imageView = new VkImageView(device, viewInfo);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                image.Dispose();
                deviceMemory.Dispose();
                imageView.Dispose();
              
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
         ~ImageData()
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
