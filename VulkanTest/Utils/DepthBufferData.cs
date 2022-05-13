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
    unsafe class DepthBufferData : ImageData
    {
        public DepthBufferData(VkPhysicalDevice physicalDevice, VkDevice device, Format format, Extent2D extent)
          : base(physicalDevice,
                device,
                format,
                extent,
                ImageTiling.Optimal,
                ImageUsageFlags.ImageUsageDepthStencilAttachmentBit,
                ImageLayout.Undefined,
                MemoryPropertyFlags.MemoryPropertyDeviceLocalBit,
                ImageAspectFlags.ImageAspectDepthBit)
        {
        }

    }
}
