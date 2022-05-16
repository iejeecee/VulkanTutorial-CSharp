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
    unsafe class VkCommandBuffer : IDisposable
    {
        readonly Vk vk;   
        CommandBuffer commandBuffer;
        Device device;
        CommandPool commandPool;
        private bool disposedValue;

        public VkCommandBuffer(CommandBuffer commandBuffer, Device device, CommandPool commandPool)
        {
            vk = Vk.GetApi();
            this.commandBuffer = commandBuffer;
            this.device = device;
            this.commandPool = commandPool;
        }

        public void Clear()
        {
            vk.FreeCommandBuffers(device, commandPool, 1, this);
        }

        public void Begin(in CommandBufferBeginInfo beginInfo)
        {
            var result = vk.BeginCommandBuffer(this, beginInfo);

            if (result != Result.Success)
            {
                throw new ResultException("Error trying begin commandbuffer");
            }
        }

        public void BeginRenderPass(in RenderPassBeginInfo renderPassBegin, SubpassContents subpassContents)
        {
            vk.CmdBeginRenderPass(this, renderPassBegin, subpassContents);
        }

        public void EndRenderPass()
        {
            vk.CmdEndRenderPass(this);
        }

        public void BindPipeline(PipelineBindPoint pipelineBindPoint, in Pipeline pipeline)
        {
            vk.CmdBindPipeline(this, pipelineBindPoint, pipeline);
        }

        public void BindIndexBuffer(VkBuffer buffer, ulong offset, IndexType indexType)
        {
            vk.CmdBindIndexBuffer(this, buffer, offset, indexType);
        }

        public void DrawIndexed(uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
        {
            vk.CmdDrawIndexed(this, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
        }

        public void BindVertexBuffers(in VkBuffer buffer, ulong offset)
        {
            vk.CmdBindVertexBuffers(this, 0, 1, buffer, offset);
        }

        public void BindVertexBuffers(uint firstBinding, ReadOnlySpan<Silk.NET.Vulkan.Buffer> buffers, ReadOnlySpan<ulong> offsets)
        {
            vk.CmdBindVertexBuffers(this, firstBinding, (uint)buffers.Length, buffers, offsets);
        }

        public void BindDescriptorSets(PipelineBindPoint pipelineBindPoint, in PipelineLayout pipelineLayout,
            uint firstSet, uint descriptorSetCount, ReadOnlySpan<DescriptorSet> descriptorSets, uint dynamicOffsetCount,
            ReadOnlySpan<uint> dynamicOffsets)
        {
            vk.CmdBindDescriptorSets(this, pipelineBindPoint, pipelineLayout,
                firstSet, descriptorSetCount, descriptorSets, dynamicOffsetCount, dynamicOffsets);
        }

        public void BindDescriptorSets(PipelineBindPoint pipelineBindPoint, in PipelineLayout pipelineLayout,
         in DescriptorSet descriptorSet, uint? dynamicOffset = null)
        {
            uint dynamicOffsetCount = dynamicOffset.HasValue ? (uint)1 : 0;

            vk.CmdBindDescriptorSets(this, pipelineBindPoint, pipelineLayout,
                0, 1, descriptorSet, dynamicOffsetCount, dynamicOffset.HasValue ? dynamicOffset.Value : 0);
        }

        public void BindDescriptorSets(PipelineBindPoint pipelineBindPoint, in PipelineLayout pipelineLayout,
          uint firstSet, ReadOnlySpan<DescriptorSet> descriptorSets, ReadOnlySpan<uint> dynamicOffsets)
        {
            vk.CmdBindDescriptorSets(this, pipelineBindPoint, pipelineLayout,
                firstSet, (uint)descriptorSets.Length, descriptorSets, (uint)dynamicOffsets.Length, dynamicOffsets);
        }


        public void End()
        {
            var result = vk.EndCommandBuffer(this);

            if (result != Result.Success)
            {
                throw new ResultException("Error trying end commandbuffer");
            }
        }

        public void Reset(CommandBufferResetFlags flags)
        {
            var result = vk.ResetCommandBuffer(this, flags);

            if (result != Result.Success)
            {
                throw new ResultException("Error trying reset commandbuffer");
            }
        }

        public void CopyBuffer(VkBuffer srcBuffer, VkBuffer dstBuffer, ReadOnlySpan<BufferCopy> regions)
        {
            vk.CmdCopyBuffer(this, srcBuffer, dstBuffer, (uint)regions.Length, regions);
        }

        public void CopyBuffer(VkBuffer srcBuffer, VkBuffer dstBuffer, in BufferCopy region)
        {
            vk.CmdCopyBuffer(this, srcBuffer, dstBuffer, 1, region);
        }

        public void CopyBufferToImage(VkBuffer srcBuffer, VkImage dstImage, ImageLayout destImageLayout, in BufferImageCopy region)
        {
            vk.CmdCopyBufferToImage(this, srcBuffer, dstImage, destImageLayout, 1, region);
        }

        public void CopyBufferToImage(VkBuffer srcBuffer, VkImage dstImage, ImageLayout destImageLayout, ReadOnlySpan<BufferImageCopy> regions)
        {
            vk.CmdCopyBufferToImage(this, srcBuffer, dstImage, destImageLayout, (uint)regions.Length, regions);
        }

        public void PipelineBarrier(PipelineStageFlags srcStageMask, PipelineStageFlags dstStageMask,
            DependencyFlags dependencyFlags, ReadOnlySpan<MemoryBarrier> memoryBarriers, ReadOnlySpan<BufferMemoryBarrier> bufferMemoryBarriers,
            ReadOnlySpan<ImageMemoryBarrier> imageMemoryBarriers)
        {
            vk.CmdPipelineBarrier(this, srcStageMask, dstStageMask, dependencyFlags, memoryBarriers, bufferMemoryBarriers, imageMemoryBarriers);
        }
     
        public static implicit operator CommandBuffer(VkCommandBuffer c) => c.commandBuffer;

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
        ~VkCommandBuffer()
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

