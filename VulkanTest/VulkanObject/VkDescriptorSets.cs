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
    unsafe class VkDescriptorSets : List<VkDescriptorSet>
    {
        public VkDescriptorSets(VkDevice device, in DescriptorSetAllocateInfo allocateInfo)
        {
            Vk vk = Vk.GetApi();

            DescriptorSet* descriptorSets = (DescriptorSet*)Mem.AllocArray<DescriptorSet>((int)allocateInfo.DescriptorSetCount);

            Result result = vk.AllocateDescriptorSets(device, allocateInfo, descriptorSets);

            if (result != Result.Success)
            {
                ResultException.Throw(result, "Error allocating descriptor sets");
            }

            for (int i = 0; i < allocateInfo.DescriptorSetCount; i++)
            {
                Add(new VkDescriptorSet(descriptorSets[i]));
            }

            Mem.FreeArray(descriptorSets);
        }
    }
}
