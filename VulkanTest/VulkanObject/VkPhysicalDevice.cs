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
       
    unsafe class VkPhysicalDevice
    {
        PhysicalDevice device;
        Vk vk;

        PhysicalDeviceMemoryProperties memProperties;
        PhysicalDeviceProperties properties;
        PhysicalDeviceFeatures supportedFeatures;
        string[] extensions;

        public VkPhysicalDevice(PhysicalDevice device)
        {
            this.device = device;
            vk = Vk.GetApi();

            vk.GetPhysicalDeviceMemoryProperties(device, out memProperties);
            vk.GetPhysicalDeviceProperties(device, out properties);
            vk.GetPhysicalDeviceFeatures(device, out supportedFeatures);
            
        }
  
        public FormatProperties GetFormatProperties(Format format)
        {
            vk.GetPhysicalDeviceFormatProperties(device, format,
                out FormatProperties props);

            return props;
        }

        public void GetQueueFamilyProperties(uint* queueFamilyPropertyCount, Span<QueueFamilyProperties> queueFamilyProperties)
        {
            vk.GetPhysicalDeviceQueueFamilyProperties(device, queueFamilyPropertyCount, queueFamilyProperties);
        }

        public bool GetSurfaceSupportKHR(VkSurfaceKHR surface, uint queueFamilyIndex)
        {
            KhrSurface khrSurface = surface.KhrSurface;
         
            Result result = khrSurface.GetPhysicalDeviceSurfaceSupport(device, queueFamilyIndex, surface, out Bool32 presentSupport);
            if (result != Result.Success)
            {
                throw new ResultException(nameof(GetSurfaceSupportKHR));
            }

            return presentSupport;
        }

        public SurfaceFormatKHR[] GetSurfaceFormatsKHR(VkSurfaceKHR surface)
        {
            KhrSurface khrSurface = surface.KhrSurface;
        
            uint surfaceFormatCount;

            Result result = khrSurface.GetPhysicalDeviceSurfaceFormats(device, surface, &surfaceFormatCount, null);
            if (result != Result.Success)
            {
                throw new ResultException(nameof(GetSurfaceFormatsKHR));
            }

            SurfaceFormatKHR[] surfaceFormats = new SurfaceFormatKHR[surfaceFormatCount];

            result = khrSurface.GetPhysicalDeviceSurfaceFormats(device, surface, &surfaceFormatCount, surfaceFormats);
            if (result != Result.Success)
            {
                throw new ResultException(nameof(GetSurfaceFormatsKHR));
            }

            return surfaceFormats;
        }

        public SurfaceCapabilitiesKHR GetSurfaceCapabilitiesKHR(VkSurfaceKHR surface)
        {
            KhrSurface khrSurface = surface.KhrSurface;
         
            Result result = khrSurface.GetPhysicalDeviceSurfaceCapabilities(device, surface, out SurfaceCapabilitiesKHR surfaceCapabilities);
            if (result != Result.Success)
            {
                throw new ResultException(nameof(GetSurfaceCapabilitiesKHR));
            }

            return surfaceCapabilities;
        }

        public PresentModeKHR[] GetSurfacePresentModesKHR(VkSurfaceKHR surface)
        {
            KhrSurface khrSurface = surface.KhrSurface;
           
            uint presentModesCount;

            Result result = khrSurface.GetPhysicalDeviceSurfacePresentModes(device, surface, &presentModesCount, null);
            if (result != Result.Success)
            {
                throw new ResultException(nameof(GetSurfacePresentModesKHR));
            }

            PresentModeKHR[] presentModes = new PresentModeKHR[presentModesCount];

            result = khrSurface.GetPhysicalDeviceSurfacePresentModes(device, surface, &presentModesCount, presentModes);
            if (result != Result.Success)
            {
                throw new ResultException(nameof(GetSurfacePresentModesKHR));
            }

            return presentModes;
        }

        public PhysicalDeviceFeatures SupportedFeatures
        {
            get
            {               
                return supportedFeatures;
            }
        }

        public PhysicalDeviceProperties Properties
        {
            get
            {               
                return properties;
            }
        }

        public PhysicalDeviceMemoryProperties MemoryProperties
        {
            get
            {                
                return memProperties;
            }
        }

        public string[] EnumerateExtensionProperties()
        {
            uint extensionCount;
            vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, null);

            ExtensionProperties[] availableExtensions = new ExtensionProperties[extensionCount];
            vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, availableExtensions);

            extensions = new string[extensionCount];

            for (int i = 0; i < extensionCount; i++)
            {
                var extension = availableExtensions[i];

                extensions[i] = Marshal.PtrToStringAnsi((nint)extension.ExtensionName);
            }

            return extensions;
        }

        public static implicit operator PhysicalDevice(VkPhysicalDevice d) => d.device;
             
    }
}
