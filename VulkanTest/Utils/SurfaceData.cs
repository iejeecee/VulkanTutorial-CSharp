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
using VulkanTest.VulkanObject;

namespace VulkanTest.Utils
{
    unsafe class SurfaceData : IDisposable
    {
        VkSurfaceKHR surface;   
        Vk vk;
        Extent2D extent;
        private bool disposedValue;

        public SurfaceData(IWindow window, VkInstance instance)
        {           
            vk = Vk.GetApi();
            Extent = new Extent2D((uint)window.FramebufferSize.X, (uint)window.FramebufferSize.Y);

            surface = new VkSurfaceKHR(window, instance);
            
        }

        public static void CreateWindow(string windowName, Extent2D size,
            out IWindow window, out string[] requiredExtensions)
        {
            var opts = WindowOptions.DefaultVulkan;
            opts.Title = windowName;
            opts.Size = new Vector2D<int>((int)size.Width, (int)size.Height);

            // Uncomment the line below to use SDL
            // Window.PrioritizeSdl();

            window = Window.Create(opts);
            window.Initialize(); // For safety the window should be initialized before querying the VkSurface

            if (window.VkSurface is null)
            {
                throw new NotSupportedException("Windowing platform doesn't support Vulkan.");
            }

            var reqExtensions = window.VkSurface.GetRequiredExtensions(out uint nrReqExtensions);

            requiredExtensions = new string[nrReqExtensions];
            for (var i = 0; i < nrReqExtensions; i++)
            {
                requiredExtensions[i] = Marshal.PtrToStringUTF8((nint)reqExtensions[i]);
            }

        }

        public VkSurfaceKHR Surface { get => surface; protected set => surface = value; }      
        public Extent2D Extent { get => extent; protected set => extent = value; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                surface.Dispose();
           
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~SurfaceData()
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
