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
    unsafe class VkPipeline : IDisposable
    {
        Pipeline pipeline;
        readonly Vk vk;
        readonly VkDevice device;
        private bool disposedValue;

        public VkPipeline(VkDevice device, PipelineCache pipelineCache, in GraphicsPipelineCreateInfo pipelineInfo)
        {
            vk = Vk.GetApi();
            this.device = device;
           
            Result result = vk.CreateGraphicsPipelines(device, pipelineCache, 1, in pipelineInfo, null, out pipeline);

            if (result != Result.Success)
            {
                ResultException.Throw(result, "Error creating graphics pipeline");
            }
        }

        public VkPipeline(VkDevice device, PipelineCache pipelineCache, in ComputePipelineCreateInfo pipelineInfo)
        {
            vk = Vk.GetApi();
            this.device = device;

            Result result = vk.CreateComputePipelines(device, pipelineCache, 1, in pipelineInfo, null, out pipeline);

            if (result != Result.Success)
            {
                ResultException.Throw(result, "Error creating compute pipeline");
            }
        }

        public static implicit operator Pipeline(VkPipeline p) => p.pipeline;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                vk.DestroyPipeline(device, this, null);

                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~VkPipeline()
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


