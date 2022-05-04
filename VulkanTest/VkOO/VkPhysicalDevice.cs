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

namespace VulkanTest.VkOO
{
    struct SwapChainSupportDetails
    {
        public SurfaceCapabilitiesKHR Capabilities;
        public SurfaceFormatKHR[] Formats;
        public PresentModeKHR[] PresentModes;
    }

    struct QueueFamilyIndices
    {
        public uint? GraphicsFamily { get; set; }
        public uint? PresentFamily { get; set; }

        public bool IsComplete
        {
            get
            {
                return GraphicsFamily.HasValue && PresentFamily.HasValue;
            }
        }
    }

    unsafe class VkPhysicalDevice
    {
        PhysicalDevice device;
        Vk vk;

        PhysicalDeviceMemoryProperties memProperties;
        PhysicalDeviceProperties properties;
        PhysicalDeviceFeatures supportedFeatures;
        string[] extensions;

        public VkPhysicalDevice(PhysicalDevice device, Vk vk)
        {
            this.device = device;
            this.vk = vk;

            vk.GetPhysicalDeviceMemoryProperties(device, out memProperties);
            vk.GetPhysicalDeviceProperties(device, out properties);
            vk.GetPhysicalDeviceFeatures(device, out supportedFeatures);

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
        }
       
        public QueueFamilyIndices FindGraphicsAndPresentQueueFamilyIndex(VkSurfaceData surfaceData)
        {
            KhrSurface khrSurface = surfaceData.KhrSurface;
            SurfaceKHR surface = surfaceData.Surface;

            QueueFamilyIndices queueFamilyIndices = new();

            uint queueFamilyCount = 0;
            vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);

            QueueFamilyProperties[] queueFamilies = new QueueFamilyProperties[queueFamilyCount];
            vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, queueFamilies);

            uint i = 0;
            foreach (var queueFamily in queueFamilies)
            {
                if (queueFamily.QueueFlags.HasFlag(QueueFlags.QueueGraphicsBit))
                {
                    queueFamilyIndices.GraphicsFamily = i;
                }

                khrSurface.GetPhysicalDeviceSurfaceSupport(device, i, surface, out Bool32 presentSupport);

                if (presentSupport)
                {
                    queueFamilyIndices.PresentFamily = i;
                }

                if (queueFamilyIndices.IsComplete)
                {
                    break;
                }

                i++;
            }

            return queueFamilyIndices;
        }

        public SwapChainSupportDetails QuerySwapChainSupport(VkSurfaceData surfaceData)
        {
            KhrSurface khrSurface = surfaceData.KhrSurface;
            SurfaceKHR surface = surfaceData.Surface;

            SwapChainSupportDetails details = new();

            khrSurface.GetPhysicalDeviceSurfaceCapabilities(device, surface, out details.Capabilities);

            uint formatCount;
            khrSurface.GetPhysicalDeviceSurfaceFormats(device, surface, &formatCount, null);

            if (formatCount > 0)
            {
                details.Formats = new SurfaceFormatKHR[formatCount];
                khrSurface.GetPhysicalDeviceSurfaceFormats(device, surface, &formatCount, details.Formats);
            }

            uint presentModeCount;
            khrSurface.GetPhysicalDeviceSurfacePresentModes(device, surface, &presentModeCount, null);

            if (presentModeCount != 0)
            {
                details.PresentModes = new PresentModeKHR[presentModeCount];
                khrSurface.GetPhysicalDeviceSurfacePresentModes(device, surface, &presentModeCount, details.PresentModes);
            }

            return details;
        }


        public bool CheckExtensionsSupported(string[] deviceExtensions)
        {            
            HashSet<string> requiredExtensions = new(deviceExtensions);

            foreach (var extension in extensions)
            {
                requiredExtensions.Remove(extension);
            }

            return requiredExtensions.Count == 0;
        }

        public FormatProperties GetFormatProperties(Format format)
        {
            vk.GetPhysicalDeviceFormatProperties(device, format,
                out FormatProperties props);

            return props;
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

        public string[] AvailableExtensions
        {
            get
            {                
                return extensions;
            }
        }

        public PhysicalDevice Device { get => device; protected set => device = value; }
        public Vk Vk { get => vk; protected set => vk = value; }
    }
}
