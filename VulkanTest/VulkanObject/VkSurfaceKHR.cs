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
    unsafe class VkSurfaceKHR : IDisposable
    {
        SurfaceKHR surface;
        KhrSurface khrSurface;
        VkInstance instance;
        private bool disposedValue;

        public VkSurfaceKHR(IWindow window, VkInstance instance)
        {
            Vk vk = Vk.GetApi();
            this.instance = instance;

            surface = window.VkSurface.Create<AllocationCallbacks>(((Instance)instance).ToHandle(), null).ToSurface();

            if (!vk.TryGetInstanceExtension(this.instance, out khrSurface))
            {
                throw new NotSupportedException("KHR_surface extension not found.");
            }

        }

        public void Clear()
        {
            khrSurface.DestroySurface(instance, surface, null);
        }

        public static implicit operator SurfaceKHR(VkSurfaceKHR s) => s.surface;    
        public KhrSurface KhrSurface { get => khrSurface; set => khrSurface = value; }

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
        ~VkSurfaceKHR()
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
