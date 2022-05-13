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
    unsafe class VkDebugUtilsMessengerEXT : IDisposable
    {
        ExtDebugUtils debugUtils;
        DebugUtilsMessengerEXT debugMessenger;
        private bool disposedValue;
        VkInstance instance;

        public ExtDebugUtils DebugUtils { get => debugUtils; protected set => debugUtils = value; }

        public VkDebugUtilsMessengerEXT(VkInstance instance, in DebugUtilsMessengerCreateInfoEXT createInfo)
        {
            Vk vk = Vk.GetApi();
            this.instance = instance;

            if (!vk.TryGetInstanceExtension(instance, out debugUtils))
            {
                throw new Exception("Failed to create debug messenger.");
            }
           
            if (debugUtils.CreateDebugUtilsMessenger(instance, in createInfo, null,
                out debugMessenger) != Result.Success)
            {
                throw new Exception("Failed to create debug messenger.");
            }
            
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }
              
                debugUtils.DestroyDebugUtilsMessenger(instance, debugMessenger, null);               
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
         ~VkDebugUtilsMessengerEXT()
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
