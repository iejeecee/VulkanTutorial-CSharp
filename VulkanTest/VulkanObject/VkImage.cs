﻿using System;
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
    unsafe class VkImage : IDisposable
    {
        Vk vk;
        VkDevice device;

        Image image;
        private bool disposedValue;

        public VkImage(VkDevice device, Image image)
        {
            vk = Vk.GetApi();
            this.device = device;

            this.image = image;
        }

        public VkImage(VkDevice device, in ImageCreateInfo createInfo)
        {
            vk = Vk.GetApi();
            this.device = device;

            Result result = vk.CreateImage(device, createInfo, null, out image);

            if (result != Result.Success)
            {
                throw new ResultException("Error creating image");
            }
         
        }

        public void BindMemory(VkDeviceMemory memory, ulong memoryOffset = 0)
        {
            vk.BindImageMemory(device, this, memory, memoryOffset);
        }

        public MemoryRequirements GetMemoryRequirements()
        {
            vk.GetImageMemoryRequirements(device, this, out MemoryRequirements memoryRequirements);

            return memoryRequirements;
        }
     
        public static implicit operator Image(VkImage i) => i.image;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                vk.DestroyImage(device, image, null);
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~VkImage()
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
