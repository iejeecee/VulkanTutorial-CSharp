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
        readonly Vk vk;
          
        string[] extensions;

        public VkPhysicalDevice(PhysicalDevice device)
        {
            this.device = device;
            vk = Vk.GetApi();                                
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
                ResultException.Throw(result, "Error getting supported surfaces by physical device");
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
                ResultException.Throw(result, "Error getting physical device supported surface formats");
            }

            SurfaceFormatKHR[] surfaceFormats = new SurfaceFormatKHR[surfaceFormatCount];

            result = khrSurface.GetPhysicalDeviceSurfaceFormats(device, surface, &surfaceFormatCount, surfaceFormats);
            if (result != Result.Success)
            {
                ResultException.Throw(result, "Error getting physical device supported surface formats");
            }

            return surfaceFormats;
        }

        public SurfaceCapabilitiesKHR GetSurfaceCapabilitiesKHR(VkSurfaceKHR surface)
        {
            KhrSurface khrSurface = surface.KhrSurface;
         
            Result result = khrSurface.GetPhysicalDeviceSurfaceCapabilities(device, surface, out SurfaceCapabilitiesKHR surfaceCapabilities);
            if (result != Result.Success)
            {
                ResultException.Throw(result, "Error getting physical device supported surface capabilities");
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
                ResultException.Throw(result, "Error getting physical device supported surface present modes");
            }

            PresentModeKHR[] presentModes = new PresentModeKHR[presentModesCount];

            result = khrSurface.GetPhysicalDeviceSurfacePresentModes(device, surface, &presentModesCount, presentModes);
            if (result != Result.Success)
            {
                ResultException.Throw(result, "Error getting physical device supported surface present modes");
            }

            return presentModes;
        }

        public PhysicalDeviceFeatures GetSupportedFeatures()
        {
            vk.GetPhysicalDeviceFeatures(device, out PhysicalDeviceFeatures supportedFeatures);

            return supportedFeatures;            
        }

        public PhysicalDeviceProperties GetProperties()
        {
            vk.GetPhysicalDeviceProperties(device, out PhysicalDeviceProperties properties);

            return properties;
        }

        public PhysicalDeviceMemoryProperties GetMemoryProperties()
        {            
            vk.GetPhysicalDeviceMemoryProperties(device, out PhysicalDeviceMemoryProperties memProperties);

            return memProperties;
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
