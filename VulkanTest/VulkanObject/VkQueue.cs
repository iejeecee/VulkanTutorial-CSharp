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
    unsafe class VkQueue
    {
        Queue queue;
        readonly Vk vk;

        public VkQueue(Queue queue)
        {
            vk = Vk.GetApi();
            this.queue = queue;
        }

        public void PresentKHR(VkSwapChainKHR swapchain, in PresentInfoKHR presentInfo)
        {
            KhrSwapchain khrSwapchain = swapchain.KhrSwapchain;

            khrSwapchain.QueuePresent(this, in presentInfo);
        }

        public void Submit(SubmitInfo submit, VkFence fence)
        {
            var result = vk.QueueSubmit(this, 1, submit, fence ?? new Fence(null));

            if (result != Result.Success)
            {
                throw new ResultException("Error submitting queue");
            }
        }

        public void Submit(Span<SubmitInfo> submits, VkFence fence)
        {
            var result = vk.QueueSubmit(this, submits, fence ?? new Fence(null));

            if (result != Result.Success)
            {
                throw new ResultException("Error submitting queue");
            }
        }

        public void WaitIdle()
        {
            vk.QueueWaitIdle(this);
        }

        public static implicit operator Queue(VkQueue q) => q.queue;
    }
}
