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
using VulkanTest.VulkanObject;

namespace VulkanTest.Utils
{
    unsafe class SU
    {
        public static DebugUtilsMessengerCreateInfoEXT MakeDebugUtilsMessengerCreateInfoEXT()
        {
            DebugUtilsMessengerCreateInfoEXT createInfo = new()
            {
                SType = StructureType.DebugUtilsMessengerCreateInfoExt,
                MessageSeverity =
                    DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt |
                    DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityInfoBitExt |
                    DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt |
                    DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt,
                MessageType =
                    DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt |
                    DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeValidationBitExt |
                    DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt,
                PfnUserCallback = new PfnDebugUtilsMessengerCallbackEXT(MessageCallback),
                PUserData = null
            };

            return createInfo;
        }

        public static uint MessageCallback(DebugUtilsMessageSeverityFlagsEXT severity, DebugUtilsMessageTypeFlagsEXT messageType, DebugUtilsMessengerCallbackDataEXT* callbackData, void* userData)
        {
            string messageSeverity;

            if (severity < DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt) return Vk.False;

            if (severity.HasFlag(DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt))
            {
                messageSeverity = "error";
            }
            else if (severity.HasFlag(DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityInfoBitExt))
            {
                messageSeverity = "info";
            }
            else if (severity.HasFlag(DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt))
            {
                messageSeverity = "verbose";
            }
            else
            {
                messageSeverity = "warning";
            }

            string messageTypeStr;

            if (messageType.HasFlag(DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt))
            {
                messageTypeStr = "";
            }
            else if (messageType.HasFlag(DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt))
            {
                messageTypeStr = "performance ";
            }
            else
            {
                messageTypeStr = "validation ";
            }

            Debug.Print($"VK ({messageTypeStr}{messageSeverity}): {Marshal.PtrToStringAnsi(new IntPtr(callbackData->PMessage))}");

            return Vk.False;
        }

        public static QueueFamilyIndices FindGraphicsAndPresentQueueFamilyIndex(VkPhysicalDevice device, SurfaceData surfaceData)
        {
            VkSurfaceKHR surface = surfaceData.Surface;

            QueueFamilyIndices queueFamilyIndices = new();

            uint queueFamilyCount = 0;
            device.GetQueueFamilyProperties(&queueFamilyCount, null);

            QueueFamilyProperties[] queueFamilies = new QueueFamilyProperties[queueFamilyCount];
            device.GetQueueFamilyProperties(&queueFamilyCount, queueFamilies);

            uint i = 0;
            foreach (var queueFamily in queueFamilies)
            {
                if (queueFamily.QueueFlags.HasFlag(QueueFlags.QueueGraphicsBit))
                {
                    queueFamilyIndices.GraphicsFamily = i;
                }

                bool isPresentSupport = device.GetSurfaceSupportKHR(surface, i);

                if (isPresentSupport)
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

        public static VkPhysicalDevice PickPhysicalDevice(VkInstance instance, SurfaceData surfaceData, string[] deviceExtensions)
        {
            VkPhysicalDevice pickedDevice = instance.EnumerateDevices().FirstOrDefault();
           
            foreach (var device in instance.EnumerateDevices())
            {
                string[] availableExtensions = device.EnumerateExtensionProperties();

                bool isExtensionsSupported = deviceExtensions.All(e => availableExtensions.Contains(e));

                var indices = FindGraphicsAndPresentQueueFamilyIndex(device, surfaceData);

                bool isSwapChainAdequate = false;
                if (isExtensionsSupported)
                {
                    var formats = device.GetSurfaceFormatsKHR(surfaceData.Surface);
                    var presentModes = device.GetSurfacePresentModesKHR(surfaceData.Surface);

                    isSwapChainAdequate = (formats != null) && (presentModes != null);
                }

                if (indices.IsComplete && isExtensionsSupported && isSwapChainAdequate
                    && device.SupportedFeatures.SamplerAnisotropy)
                {
                    pickedDevice = device;
                }
            }

            return pickedDevice;
        }


        public static VkDevice CreateDevice(
            VkInstance instance,
            VkPhysicalDevice physicalDevice,
            QueueFamilyIndices indices,
            PhysicalDeviceFeatures deviceFeatures,
            string[] validationLayers = null,
            string[] deviceExtensions = null)
        {
            float queuePriority = 1.0f;
            Vk vk = Vk.GetApi();

            HashSet<uint> uniqueQueueFamilies = new() { indices.GraphicsFamily.Value, indices.PresentFamily.Value };

            using var mem = GlobalMemory.Allocate(uniqueQueueFamilies.Count * sizeof(DeviceQueueCreateInfo));
            var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

            int i = 0;

            foreach (var queueId in uniqueQueueFamilies)
            {
                DeviceQueueCreateInfo queueCreateInfo = new()
                {
                    SType = StructureType.DeviceQueueCreateInfo,
                    QueueFamilyIndex = queueId,
                    QueueCount = 1,
                    PQueuePriorities = &queuePriority
                };

                queueCreateInfos[i++] = queueCreateInfo;
            };

            DeviceCreateInfo createInfo = new()
            {
                SType = StructureType.DeviceCreateInfo,
                PQueueCreateInfos = queueCreateInfos,
                QueueCreateInfoCount = (uint)uniqueQueueFamilies.Count,
                PEnabledFeatures = &deviceFeatures,
            };

            if (deviceExtensions != null)
            {
                createInfo.EnabledExtensionCount = (uint)deviceExtensions.Length;
                createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions);
            }

            if (validationLayers != null)
            {
                createInfo.EnabledLayerCount = (uint)validationLayers.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);
            }

            if (vk.CreateDevice(physicalDevice, in createInfo, null, out Device device) != Result.Success)
            {
                throw new ResultException(nameof(CreateDevice));
            }

            return new VkDevice(instance, device);
        }

        public static SurfaceFormatKHR PickSurfaceFormat(SurfaceFormatKHR[] formats)
        {        
            SurfaceFormatKHR pickedFormat = formats[0];
            if (formats.Length == 1)
            {
                if (formats[0].Format == Format.Undefined)
                {
                    pickedFormat.Format = Format.B8G8R8A8Unorm;
                    pickedFormat.ColorSpace = ColorSpaceKHR.ColorspaceSrgbNonlinearKhr;
                }
            }
            else
            {
                // request several formats, the first found will be used
                Format[] requestedFormats = new Format[] { Format.B8G8R8A8Srgb, Format.R8G8B8A8Srgb, Format.B8G8R8Unorm, Format.R8G8B8Unorm };
                ColorSpaceKHR requestedColorSpace = ColorSpaceKHR.ColorspaceSrgbNonlinearKhr;
                for (int i = 0; i < requestedFormats.Length; i++)
                {
                    Format requestedFormat = requestedFormats[i];

                    if (formats.Any(f => f.Format == requestedFormat && f.ColorSpace == requestedColorSpace))
                    {
                        pickedFormat.Format = requestedFormat;
                        pickedFormat.ColorSpace = requestedColorSpace;
                        break;
                    }

                }               
            }

            return pickedFormat;
        }

        public static PresentModeKHR PickPresentMode(PresentModeKHR[] availablePresentModes)
        {
            PresentModeKHR pickedMode = PresentModeKHR.PresentModeFifoKhr;

            foreach (var availablePresentMode in availablePresentModes)
            {
                if (availablePresentMode == PresentModeKHR.PresentModeMailboxKhr)
                {
                    return availablePresentMode;
                }

                if (availablePresentMode == PresentModeKHR.PresentModeImmediateKhr)
                {
                    pickedMode = PresentModeKHR.PresentModeImmediateKhr;
                }
            }

            return pickedMode;
        }

    }
}
