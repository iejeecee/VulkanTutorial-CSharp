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
    unsafe class VkDevice : IDisposable
    {
        readonly VkInstance instance;     
        readonly Vk vk;
        Device device;
        private bool disposedValue;

        public VkInstance Instance => instance;

        public VkDevice(VkInstance instance, Device device)           
        {          
            vk = Vk.GetApi();
          
            this.device = device;
            this.instance = instance;
        }

        public void WaitIdle()
        {
            vk.DeviceWaitIdle(device);
        }

        public VkQueue GetQueue(uint queueFamilyIndex, uint queueIndex = 0)
        {
            vk.GetDeviceQueue(device, queueFamilyIndex, queueIndex, out Queue queue);

            return new VkQueue(queue);
        }
                                  
        public static implicit operator Device(VkDevice d) => d.device;
             
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                vk.DestroyDevice(device, null);

                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~VkDevice()
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