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
    unsafe class VkDescriptorSet
    {
        DescriptorSet descriptorSet;

        public VkDescriptorSet(DescriptorSet descriptorSet)
        {
            this.descriptorSet = descriptorSet;
        }

        public static implicit operator DescriptorSet(VkDescriptorSet d) => d.descriptorSet;
    }
}
