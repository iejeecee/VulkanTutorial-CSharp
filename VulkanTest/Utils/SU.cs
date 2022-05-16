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

        public static VkDeviceMemory AllocateDeviceMemory(VkDevice device,
                                           PhysicalDeviceMemoryProperties memoryProperties,
                                           MemoryRequirements memoryRequirements,
                                           MemoryPropertyFlags memoryPropertyFlags)
        {
            uint memoryTypeIndex = FindMemoryType(memoryProperties, memoryRequirements.MemoryTypeBits, memoryPropertyFlags);

            MemoryAllocateInfo allocInfo = new
            (                         
                allocationSize: memoryRequirements.Size,
                memoryTypeIndex: memoryTypeIndex
            );

            return new VkDeviceMemory(device, allocInfo);
        }


        public static DebugUtilsMessengerCreateInfoEXT MakeDebugUtilsMessengerCreateInfoEXT()
        {
            DebugUtilsMessengerCreateInfoEXT createInfo = new
            (
                messageSeverity:
                    DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt |
                    DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityInfoBitExt |
                    DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt |
                    DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt,
                messageType:
                    DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt |
                    DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeValidationBitExt |
                    DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt,
                pfnUserCallback: new PfnDebugUtilsMessengerCallbackEXT(MessageCallback)
            );

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
                    && device.GetSupportedFeatures().SamplerAnisotropy)
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
                DeviceQueueCreateInfo queueCreateInfo = new
                (                                   
                    queueFamilyIndex: queueId,
                    queueCount: 1,
                    pQueuePriorities: &queuePriority
                );

                queueCreateInfos[i++] = queueCreateInfo;
            };

            DeviceCreateInfo createInfo = new
            (             
                pQueueCreateInfos: queueCreateInfos,
                queueCreateInfoCount: (uint)uniqueQueueFamilies.Count,
                pEnabledFeatures: &deviceFeatures
            );

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
                throw new ResultException("Error creating logical device");
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

        static uint FindMemoryType(PhysicalDeviceMemoryProperties memoryProperties, uint typeBits, MemoryPropertyFlags requirementsMask)
        {
            for (int i = 0; i < memoryProperties.MemoryTypeCount; i++)
            {
                if ((typeBits & (i << 1)) != 0 && memoryProperties.MemoryTypes[i].PropertyFlags.HasFlag(requirementsMask))
                {
                    return (uint)i;

                }
            }

            throw new Exception("failed to find suitable memory type!");
        }


        public static void OneTimeSubmit(VkCommandBuffer commandBuffer, VkQueue queue, Action<VkCommandBuffer> func)
        {
            commandBuffer.Begin(new(flags: CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit));
            func(commandBuffer);
            commandBuffer.End();

            CommandBuffer buffer = commandBuffer;

            SubmitInfo submitInfo = new(commandBufferCount: 1, pCommandBuffers: &buffer);
            queue.Submit(submitInfo, null);
            queue.WaitIdle();
        }

        public static void OneTimeSubmit(VkDevice device, VkCommandPool commandPool, VkQueue queue, Action<VkCommandBuffer> func)
        {
            VkCommandBuffers commandBuffers = new(device,
                new(commandPool: commandPool, level: CommandBufferLevel.Primary, commandBufferCount: 1));

            var commandBuffer = commandBuffers.FirstOrDefault();

            OneTimeSubmit(commandBuffer, queue, func);

            commandBuffer.Dispose();
        }

        public static void CopyToDevice<T>(VkDeviceMemory deviceMemory, Span<T> data, uint stride = 0) where T : struct
        {
            Debug.Assert(!data.IsEmpty);

            uint elemSize = (uint)Marshal.SizeOf(data[0]);

            stride = stride > 0 ? stride : elemSize;
            Debug.Assert(elemSize <= stride);

            byte* dataPtr = (byte*)Unsafe.AsPointer(ref data.GetPinnableReference());

            byte* deviceData = (byte*)deviceMemory.MapMemory(0, (ulong)data.Length * stride);
            if (stride == elemSize)
            {
                ulong sizeBytes = (ulong)data.Length * stride;

                System.Buffer.MemoryCopy(dataPtr, deviceData, sizeBytes, sizeBytes);
            }
            else
            {
                for (int i = 0; i < data.Length; i++)
                {
                    System.Buffer.MemoryCopy(dataPtr, deviceData, elemSize, elemSize);
                    dataPtr += elemSize;
                    deviceData += stride;
                }
            }

            deviceMemory.UnmapMemory();
        }


        public static void SetImageLayout(VkCommandBuffer commandBuffer, VkImage image, Format format, ImageLayout oldImageLayout, ImageLayout newImageLayout)
        {
            AccessFlags sourceAccessMask = 0;

            switch (oldImageLayout)
            {
                case ImageLayout.TransferDstOptimal:
                    {
                        sourceAccessMask = AccessFlags.AccessTransferWriteBit; 
                        break;
                    }
                case ImageLayout.Preinitialized:
                    {
                        sourceAccessMask = AccessFlags.AccessHostWriteBit; break;
                    }
                case ImageLayout.General:  // sourceAccessMask is empty
                case ImageLayout.Undefined:
                    {
                        break;
                    }
                default:
                    {
                        Debug.Assert(false);
                        break;
                    }
            }

            PipelineStageFlags sourceStage = 0;

            switch (oldImageLayout)
            {
                case ImageLayout.General:
                case ImageLayout.Preinitialized:
                    {
                        sourceStage = PipelineStageFlags.PipelineStageHostBit; 
                        break;
                    }
                case ImageLayout.TransferDstOptimal:
                    {
                        sourceStage = PipelineStageFlags.PipelineStageTransferBit;
                        break;
                    }
                case ImageLayout.Undefined:
                    {
                        sourceStage = PipelineStageFlags.PipelineStageTopOfPipeBit; 
                        break;
                    }
                default:
                    {
                        Debug.Assert(false);
                        break;
                    }
            }

            AccessFlags destinationAccessMask = 0;

            switch (newImageLayout)
            {
                case ImageLayout.ColorAttachmentOptimal:
                    {
                        destinationAccessMask = AccessFlags.AccessColorAttachmentWriteBit; 
                        break;
                    }
                case ImageLayout.DepthStencilAttachmentOptimal:
                    {
                        destinationAccessMask = AccessFlags.AccessDepthStencilAttachmentReadBit | AccessFlags.AccessDepthStencilAttachmentWriteBit;
                        break;
                    }
                case ImageLayout.General:  // empty destinationAccessMask
                case ImageLayout.PresentSrcKhr:
                    {
                        break;
                    }
                case ImageLayout.ShaderReadOnlyOptimal:
                    {
                        destinationAccessMask = AccessFlags.AccessShaderReadBit;
                        break;
                    }
                case ImageLayout.TransferSrcOptimal:
                    {
                        destinationAccessMask = AccessFlags.AccessTransferReadBit;
                        break;
                    }
                case ImageLayout.TransferDstOptimal:
                    {
                        destinationAccessMask = AccessFlags.AccessTransferWriteBit;
                        break;
                    }
                default:
                    {
                        Debug.Assert(false);
                        break;
                    }
            }

            PipelineStageFlags destinationStage = 0;

            switch (newImageLayout)
            {
                case ImageLayout.ColorAttachmentOptimal:
                    {
                        destinationStage = PipelineStageFlags.PipelineStageColorAttachmentOutputBit; 
                        break;
                    }
                case ImageLayout.DepthStencilAttachmentOptimal:
                    {
                        destinationStage = PipelineStageFlags.PipelineStageEarlyFragmentTestsBit;
                        break;
                    }
                case ImageLayout.General:
                    {
                        destinationStage = PipelineStageFlags.PipelineStageHostBit;
                        break;
                    }
                case ImageLayout.PresentSrcKhr:
                    {
                        destinationStage = PipelineStageFlags.PipelineStageBottomOfPipeBit;
                        break;
                    }
                case ImageLayout.ShaderReadOnlyOptimal:
                    {
                        destinationStage = PipelineStageFlags.PipelineStageFragmentShaderBit;
                        break;
                    }
                case ImageLayout.TransferDstOptimal:
                case ImageLayout.TransferSrcOptimal:
                    {
                        destinationStage = PipelineStageFlags.PipelineStageTransferBit;
                        break;
                    }
                default:
                    {
                        Debug.Assert(false);
                        break;
                    }
            }

            ImageAspectFlags aspectMask = ImageAspectFlags.ImageAspectColorBit;

            if (newImageLayout == ImageLayout.DepthStencilAttachmentOptimal)
            {
                aspectMask = ImageAspectFlags.ImageAspectDepthBit;
                
                if (format == Format.D32SfloatS8Uint || format == Format.D24UnormS8Uint)
                {
                    aspectMask |= ImageAspectFlags.ImageAspectStencilBit;
                }
            }
           
            ImageSubresourceRange imageSubresourceRange = new(aspectMask, 0, 1, 0, 1 );
            ImageMemoryBarrier imageMemoryBarrier = new
            (
                srcAccessMask: sourceAccessMask,
                dstAccessMask: destinationAccessMask,
                oldLayout: oldImageLayout,
                newLayout: newImageLayout,
                srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
                dstQueueFamilyIndex: Vk.QueueFamilyIgnored,
                image: image,
                subresourceRange: imageSubresourceRange
            );

            var imageMemoryBarriers = new ReadOnlySpan<ImageMemoryBarrier>(new[] { imageMemoryBarrier });

            commandBuffer.PipelineBarrier(sourceStage, destinationStage, 0, null, null, imageMemoryBarriers);
        }
    }
}
