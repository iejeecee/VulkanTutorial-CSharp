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

            string messageIdName = Marshal.PtrToStringAnsi((nint)callbackData->PMessageIdName);
            int messageIdNumber = callbackData->MessageIdNumber;
            string message = Marshal.PtrToStringAnsi((nint)callbackData->PMessage);

            if (callbackData->QueueLabelCount > 0)
            {
                message += "\n\tQueue Labels:\n";
                for (uint i = 0; i < callbackData->QueueLabelCount; i++)
                {
                    string labelName = Marshal.PtrToStringAnsi((nint)callbackData->PQueueLabels[i].PLabelName);

                    message += $"\t\tlabelName = <${labelName}>";
                }
            }

            if (callbackData->CmdBufLabelCount > 0)
            {
                message += "\n\tCommandBuffer Labels:\n";
                for (uint i = 0; i < callbackData->CmdBufLabelCount; i++)
                {
                    string labelName = Marshal.PtrToStringAnsi((nint)callbackData->PCmdBufLabels[i].PLabelName);

                    message += $"\t\tlabelName = <${labelName}>";
                }
            }

            if (callbackData->ObjectCount > 0)
            {
                for (uint i = 0; i < callbackData->ObjectCount; i++)
                {
                    message += $"\n\tObject {i}\n";
                    message += $"\t\tobjectType = {callbackData->PObjects[i].ObjectType}\n";
                    message += $"\t\tobjectHandle = {callbackData->PObjects[i].ObjectHandle}";
                    if (callbackData->PObjects[i].PObjectName != null)
                    {
                        string objectName = Marshal.PtrToStringAnsi((nint)callbackData->PObjects[i].PObjectName);

                        message += $"\n\t\tobjectName = <{objectName}>";
                    }
                }
            }

            Debug.Print($"VK ({messageTypeStr}{messageSeverity}) {messageIdName}: {message}");

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

            var result = vk.CreateDevice(physicalDevice, in createInfo, null, out Device device);

            if (result != Result.Success)
            {
                ResultException.Throw(result, "Error creating logical device");
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

            ImageSubresourceRange imageSubresourceRange = new(aspectMask, 0, 1, 0, 1);
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

        public static VkPipeline MakeGraphicsPipeline(VkDevice device,
                                        VkPipelineCache pipelineCache,
                                        VkShaderModule vertexShaderModule,
                                        SpecializationInfo vertexShaderSpecializationInfo,
                                        VkShaderModule fragmentShaderModule,
                                        SpecializationInfo fragmentShaderSpecializationInfo,
                                        uint vertexStride,
                                        (Format, uint)[] vertexInputAttributeFormatOffset,
                                        FrontFace frontFace,
                                        bool depthBuffered,
                                        VkPipelineLayout pipelineLayout,
                                        VkRenderPass renderPass)
        {


            PipelineShaderStageCreateInfo* pipelineShaderStageCreateInfos = stackalloc[]
            {
                new PipelineShaderStageCreateInfo(
                    stage: ShaderStageFlags.ShaderStageVertexBit,
                    module: vertexShaderModule,
                    pName: (byte*)SilkMarshal.StringToPtr("main"),
                    pSpecializationInfo: &vertexShaderSpecializationInfo),

                new PipelineShaderStageCreateInfo(
                    stage: ShaderStageFlags.ShaderStageFragmentBit,
                    module: fragmentShaderModule,
                    pName: (byte*)SilkMarshal.StringToPtr("main"),
                    pSpecializationInfo: &fragmentShaderSpecializationInfo)
            };

            PipelineVertexInputStateCreateInfo pipelineVertexInputStateCreateInfo = new(flags: 0);
            VertexInputBindingDescription vertexInputBindingDescription = new(0, vertexStride);

            if (vertexStride > 0)
            {
                VertexInputAttributeDescription* vertexInputAttributeDescriptions = stackalloc VertexInputAttributeDescription[vertexInputAttributeFormatOffset.Length];
                for (uint i = 0; i < vertexInputAttributeFormatOffset.Length; i++)
                {
                    vertexInputAttributeDescriptions[i] = new
                    (
                        location: i,
                        binding: 0,
                        format: vertexInputAttributeFormatOffset[i].Item1,
                        offset: vertexInputAttributeFormatOffset[i].Item2
                    );
                }

                pipelineVertexInputStateCreateInfo.VertexBindingDescriptionCount = 1;
                pipelineVertexInputStateCreateInfo.PVertexBindingDescriptions = &vertexInputBindingDescription;

                pipelineVertexInputStateCreateInfo.VertexAttributeDescriptionCount = (uint)vertexInputAttributeFormatOffset.Length;
                pipelineVertexInputStateCreateInfo.PVertexAttributeDescriptions = vertexInputAttributeDescriptions;
            }

            PipelineInputAssemblyStateCreateInfo pipelineInputAssemblyStateCreateInfo = new(topology: PrimitiveTopology.TriangleList);

            PipelineViewportStateCreateInfo pipelineViewportStateCreateInfo = new
            (
                viewportCount: 1,
                pViewports: null,
                scissorCount: 1,
                pScissors: null
            );

            PipelineRasterizationStateCreateInfo pipelineRasterizationStateCreateInfo = new
            (
                depthClampEnable: false,
                rasterizerDiscardEnable: false,
                polygonMode: PolygonMode.Fill,
                cullMode: CullModeFlags.CullModeBackBit,
                frontFace: frontFace,
                depthBiasEnable: false,
                depthBiasConstantFactor: 0.0f,
                depthBiasClamp: 0.0f,
                depthBiasSlopeFactor: 0.0f,
                lineWidth: 1.0f
            );

            PipelineMultisampleStateCreateInfo pipelineMultisampleStateCreateInfo = new(rasterizationSamples: SampleCountFlags.SampleCount1Bit);

            StencilOpState stencilOpState = new(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep, CompareOp.Always);

            PipelineDepthStencilStateCreateInfo pipelineDepthStencilStateCreateInfo = new
            (
                depthTestEnable: depthBuffered,
                depthWriteEnable: depthBuffered,
                depthCompareOp: CompareOp.LessOrEqual,
                depthBoundsTestEnable: false,
                stencilTestEnable: false,
                front: stencilOpState,
                back: stencilOpState
            );

            ColorComponentFlags colorComponentFlags =
                ColorComponentFlags.ColorComponentRBit |
                ColorComponentFlags.ColorComponentGBit |
                ColorComponentFlags.ColorComponentBBit |
                ColorComponentFlags.ColorComponentABit;

            PipelineColorBlendAttachmentState pipelineColorBlendAttachmentState = new(false,
                                                                         BlendFactor.Zero,
                                                                         BlendFactor.Zero,
                                                                         BlendOp.Add,
                                                                         BlendFactor.Zero,
                                                                         BlendFactor.Zero,
                                                                         BlendOp.Add,
                                                                         colorComponentFlags);



            PipelineColorBlendStateCreateInfo pipelineColorBlendStateCreateInfo = new
            (
                logicOpEnable: false,
                logicOp: LogicOp.NoOp,
                attachmentCount: 1,
                pAttachments: &pipelineColorBlendAttachmentState
            );

            pipelineColorBlendStateCreateInfo.BlendConstants[0] = 0.0f;
            pipelineColorBlendStateCreateInfo.BlendConstants[1] = 0.0f;
            pipelineColorBlendStateCreateInfo.BlendConstants[2] = 0.0f;
            pipelineColorBlendStateCreateInfo.BlendConstants[3] = 0.0f;

            DynamicState* dynamicStates = stackalloc[] { DynamicState.Viewport, DynamicState.Scissor };

            PipelineDynamicStateCreateInfo pipelineDynamicStateCreateInfo = new(dynamicStateCount: 2, pDynamicStates: dynamicStates);

            GraphicsPipelineCreateInfo graphicsPipelineCreateInfo = new
                (
                    stageCount: 2,
                    pStages: pipelineShaderStageCreateInfos,
                    pVertexInputState: &pipelineVertexInputStateCreateInfo,
                    pInputAssemblyState: &pipelineInputAssemblyStateCreateInfo,
                    pViewportState: &pipelineViewportStateCreateInfo,
                    pRasterizationState: &pipelineRasterizationStateCreateInfo,
                    pMultisampleState: &pipelineMultisampleStateCreateInfo,
                    pDepthStencilState: &pipelineDepthStencilStateCreateInfo,
                    pColorBlendState: &pipelineColorBlendStateCreateInfo,
                    pDynamicState: &pipelineDynamicStateCreateInfo,
                    layout: pipelineLayout,
                    renderPass: renderPass
                );


            var pipeline = new VkPipeline(device, pipelineCache, graphicsPipelineCreateInfo);

            SilkMarshal.Free((nint)pipelineShaderStageCreateInfos[0].PName);
            SilkMarshal.Free((nint)pipelineShaderStageCreateInfos[1].PName);

            return pipeline;
        }

        public static VkDescriptorSetLayout MakeDescriptorSetLayout(VkDevice device,
                                              (DescriptorType descriptorType, int descriptorCount, ShaderStageFlags shaderStageFlags)[] bindingData,
                                              DescriptorSetLayoutCreateFlags flags = 0)
        {
            DescriptorSetLayoutBinding* bindings = stackalloc DescriptorSetLayoutBinding[bindingData.Length];

            for (uint i = 0; i < bindingData.Length; i++)
            {
                bindings[i] = new DescriptorSetLayoutBinding(
                  i,
                  bindingData[i].descriptorType,
                  (uint)bindingData[i].descriptorCount,
                  bindingData[i].shaderStageFlags
                  );
            }

            DescriptorSetLayoutCreateInfo descriptorSetLayoutCreateInfo = new(
                flags: flags,
                bindingCount: (uint)bindingData.Length,
                pBindings: bindings);

            return new VkDescriptorSetLayout(device, descriptorSetLayoutCreateInfo);
        }

        public static Format PickDepthFormat(VkPhysicalDevice physicalDevice)
        {
            Format[] candidates = new[] { Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint };
            foreach (Format format in candidates)
            {
                FormatProperties props = physicalDevice.GetFormatProperties(format);

                if (props.OptimalTilingFeatures.HasFlag(FormatFeatureFlags.FormatFeatureDepthStencilAttachmentBit))
                {
                    return format;
                }
            }
            throw new Exception("failed to find supported format!");
        }

        public static VkRenderPass MakeRenderPass(VkDevice device,
                                    Format colorFormat,
                                    Format depthFormat,
                                    AttachmentLoadOp loadOp = AttachmentLoadOp.Clear,
                                    ImageLayout colorFinalLayout = ImageLayout.PresentSrcKhr)
        {
            List<AttachmentDescription> attachmentDescriptions = new();

            Debug.Assert(colorFormat != Format.Undefined);

            attachmentDescriptions.Add(new()
            {
                Format = colorFormat,
                Samples = SampleCountFlags.SampleCount1Bit,
                LoadOp = loadOp,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = colorFinalLayout,
            });
          
            if (depthFormat != Format.Undefined)
            {
                attachmentDescriptions.Add(new()
                {
                    Format = depthFormat,
                    Samples = SampleCountFlags.SampleCount1Bit,
                    LoadOp = loadOp,
                    StoreOp = AttachmentStoreOp.DontCare,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare,
                    InitialLayout = ImageLayout.Undefined,
                    FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
                });
                
            }
            AttachmentReference colorAttachment = new( 0, ImageLayout.ColorAttachmentOptimal);
            AttachmentReference depthAttachment = new( 1, ImageLayout.DepthStencilAttachmentOptimal);

            SubpassDescription subpassDescription = new() 
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorAttachment,
                PDepthStencilAttachment = (depthFormat != Format.Undefined) ? &depthAttachment : null
            };

            AttachmentDescription* attachments = stackalloc AttachmentDescription[attachmentDescriptions.Count];

            for (int i = 0; i < attachmentDescriptions.Count; i++)
            {
                attachments[i] = attachmentDescriptions[i];
            }

            RenderPassCreateInfo renderPassCreateInfo = new
            (
                attachmentCount: (uint)attachmentDescriptions.Count,
                pAttachments: attachments,
                subpassCount: 1,
                pSubpasses: &subpassDescription            
            );
         
            return new VkRenderPass(device, renderPassCreateInfo);
        }

        public static VkFramebuffer[] MakeFramebuffers(VkDevice device,
                                                  VkRenderPass renderPass,
                                                  VkImageView[] imageViews,
                                                  VkImageView depthImageView,
                                                  Extent2D extent)
        {
            ImageView *attachments = stackalloc ImageView[2];
            attachments[1] = depthImageView ?? new ImageView(null);
           
            FramebufferCreateInfo framebufferInfo = new
            (
                 renderPass: renderPass,
                 attachmentCount: depthImageView != null ? (uint)2 : (uint)1,
                 pAttachments: attachments,
                 width: extent.Width,
                 height: extent.Height,
                 layers: 1
            );

            VkFramebuffer[] framebuffers = new VkFramebuffer[imageViews.Length];
          
            for (int i = 0; i < imageViews.Length; i++)
            {
                attachments[0] = imageViews[i];
                framebuffers[i] = new VkFramebuffer(device, framebufferInfo);             
            }

            return framebuffers;
        }

        public static VkDescriptorPool MakeDescriptorPool(VkDevice device, DescriptorPoolSize[] poolSizes)
        {
            Debug.Assert(poolSizes.Length > 0);

            uint maxSets = poolSizes.Aggregate<DescriptorPoolSize, uint>(0, (sum, dps) => sum + dps.DescriptorCount);

            Debug.Assert(maxSets > 0);

            fixed (DescriptorPoolSize* poolSizesPtr = &poolSizes[0])
            {
                DescriptorPoolCreateInfo descriptorPoolCreateInfo = new
                (
                    poolSizeCount: (uint)poolSizes.Length,
                    pPoolSizes: poolSizesPtr,
                    maxSets: maxSets
                );

                return new VkDescriptorPool(device, descriptorPoolCreateInfo);
            }            
        }

        public static void UpdateDescriptorSets(VkDevice device,
            VkDescriptorSet descriptorSet,
            (DescriptorType descriptorType, VkBuffer buffer, ulong range, VkBufferView bufferView)[] bufferData,
            TextureData textureData,
            uint bindingOffset = 0)
        {
                     
            WriteDescriptorSet[] writeDescriptorSets = new WriteDescriptorSet[bufferData.Length + 1]; 
           
            uint dstBinding = bindingOffset;

            int i;

            for (i = 0; i < bufferData.Length; i++)
            {
                var bd = bufferData[i];

                DescriptorBufferInfo bufferInfo = new (bd.buffer, 0, bd.range);
               
                BufferView bufferView = new(null);
                if (bd.bufferView != null)
                {
                    bufferView = bd.bufferView;
                }
                
                writeDescriptorSets[i] = new
                (
                        dstSet: descriptorSet,
                        dstBinding: dstBinding++,
                        dstArrayElement: 0,
                        descriptorCount: 1,
                        descriptorType: bd.descriptorType,                       
                        pBufferInfo: &bufferInfo,
                        pTexelBufferView: &bufferView
                );
                
            }

            DescriptorImageInfo imageInfo = new(textureData.sampler, textureData.imageData.imageView, ImageLayout.ShaderReadOnlyOptimal);

            writeDescriptorSets[i] = new
            (
                    dstSet: descriptorSet,
                    dstBinding: dstBinding,
                    dstArrayElement: 0,
                    descriptorCount: 1,
                    descriptorType: DescriptorType.CombinedImageSampler,
                    pImageInfo: &imageInfo                  
            );
           
            device.UpdateDescriptorSets(writeDescriptorSets, null);
        }

        /*public static void UpdateDescriptorSets(VkDevice device,
            VkDescriptorSet descriptorSet,
            (DescriptorType descriptorType, VkBuffer buffer, ulong range, VkBufferView bufferView)[] bufferData,
            TextureData[] textureData,
            uint bindingOffset = 0)
        {

            WriteDescriptorSet[] writeDescriptorSets = new WriteDescriptorSet[bufferData.Length + 1];

            uint dstBinding = bindingOffset;

            int i;

            for (i = 0; i < bufferData.Length; i++)
            {
                var bd = bufferData[i];

                DescriptorBufferInfo bufferInfo = new(bd.buffer, 0, bd.range);

                BufferView bufferView = new(null);
                if (bd.bufferView != null)
                {
                    bufferView = bd.bufferView;
                }

                writeDescriptorSets[i] = new
                (
                        dstSet: descriptorSet,
                        dstBinding: dstBinding++,
                        dstArrayElement: 0,
                        descriptorCount: 1,
                        descriptorType: bd.descriptorType,
                        pBufferInfo: &bufferInfo,
                        pTexelBufferView: &bufferView
                );

            }

            DescriptorImageInfo imageInfo = new(textureData.sampler, textureData.imageData.imageView, ImageLayout.ShaderReadOnlyOptimal);
            writeDescriptorSets[i] = new
            (
                    dstSet: descriptorSet,
                    dstBinding: dstBinding,
                    dstArrayElement: 0,
                    descriptorType: DescriptorType.CombinedImageSampler,
                    pImageInfo: &imageInfo
            );

            device.UpdateDescriptorSets(writeDescriptorSets, null);
        }*/
    }
}
