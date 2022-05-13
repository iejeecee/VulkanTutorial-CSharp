// https://vulkan-tutorial.com/Drawing_a_triangle/Setup/Base_code
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VulkanTest.Utils;
using VulkanTest.VulkanObject;

namespace VulkanTest
{

    unsafe class VulkanTutorial
    {
        public static ExtDebugUtils debugUtils;

        const int MAX_FRAMES_IN_FLIGHT = 2;

        uint currentFrame = 0;
        bool isFramebufferResized = false;

        IWindow window;
        Vk vk;     

        VkInstance instance;
        VkDebugUtilsMessengerEXT debugMessenger;

        SurfaceData surfaceData;
        SwapChainData swapChainData;
  
        VkPhysicalDevice physicalDevice;
        VkDevice device;
       
        VkQueue graphicsQueue;
        VkQueue presentQueue;
     
        Extent2D swapChainExtent;
    
        VkFramebuffer[] swapChainFramebuffers;

        VkCommandPool commandPool;      
        VkCommandBuffer[] commandBuffers;

        RenderPass renderPass;
        DescriptorSetLayout descriptorSetLayout;
        PipelineLayout pipelineLayout;

        Pipeline graphicsPipeline;

        Semaphore[] imageAvailableSemaphores;
        Semaphore[] renderFinishedSemaphores;
        Fence[] inFlightFences;

        BufferData vertexBuffer;
        BufferData indexBuffer;
        BufferData[] uniformBuffers;

        TextureData textureData;
    
        DepthBufferData depthBuffer;
    
        DescriptorPool descriptorPool;
        DescriptorSet[] descriptorSets;

        readonly string[] validationLayers = { "VK_LAYER_KHRONOS_validation" };
      
        string[] requiredExtensions;
        readonly string[] instanceExtensions = { ExtDebugUtils.ExtensionName };
        readonly string[] deviceExtensions = { KhrSwapchain.ExtensionName };
        
        readonly Vertex[] vertices = new Vertex[]
           {
                new Vertex(new (-0.5f, -0.5f, 0.0f), new (1.0f, 0.0f, 0.0f), new (1.0f, 0.0f)),
                new Vertex(new (0.5f, -0.5f, 0.0f), new (0.0f, 1.0f, 0.0f), new (0.0f, 0.0f)),
                new Vertex(new (0.5f, 0.5f, 0.0f), new (0.0f, 0.0f, 1.0f), new (0.0f, 1.0f)),
                new Vertex(new (-0.5f, 0.5f, 0.0f), new (1.0f, 1.0f, 1.0f), new (1.0f, 1.0f)),

                new Vertex(new (-0.5f, -0.5f, -0.5f), new (1.0f, 0.0f, 0.0f), new (1.0f, 0.0f)),
                new Vertex(new (0.5f, -0.5f, -0.5f), new (0.0f, 1.0f, 0.0f), new (0.0f, 0.0f)),
                new Vertex(new (0.5f, 0.5f, -0.5f), new (0.0f, 0.0f, 1.0f), new (0.0f, 1.0f)),
                new Vertex(new (-0.5f, 0.5f, -0.5f), new (1.0f, 1.0f, 1.0f), new (1.0f, 1.0f))
           };

        readonly ushort[] indices = new ushort[] {
            0, 1, 2, 2, 3, 0,
            4, 5, 6, 6, 7, 4
        };

        void InitWindow()
        {         
            SurfaceData.CreateWindow("hello world", new Extent2D(800, 600), out window,
                out requiredExtensions);

            window.FramebufferResize += OnFramebufferResize;
        }

        void OnFramebufferResize(Vector2D<int> obj)
        {
            isFramebufferResized = true;
        }

        public void Run()
        {
            InitWindow();
            InitializeVulkan();

            window.Render += Window_Render;
            window.Run();

            device.WaitIdle();

            Cleanup();
        }

        private void Window_Render(double obj)
        {
            DrawFrame();
        }

        public void InitializeVulkan()
        {
            CreateInstance();                   
            CreateLogicalDevice();

            CreateSwapChain();         
            CreateRenderPass();
            CreateDescriptorSetLayout();
            CreateGraphicsPipeline();
            CreateCommandPool();

            CreateDepthResources();
            CreateFramebuffers();
            CreateTextureImage();                 
            CreateVertexBuffer();
            CreateIndexBuffer();
            CreateUniformBuffers();
            CreateDescriptorPool();
            CreateDescriptorSets();

            CreateCommandBuffer();
            CreateSyncObjects();
        }
                  
        void CreateInstance()
        {
            vk = Vk.GetApi();

            var extensions = requiredExtensions.Concat(instanceExtensions).ToArray();

            instance = new VkInstance("textureDemo", "none", Vk.Version11,
                extensions, validationLayers, SU.MakeDebugUtilsMessengerCreateInfoEXT());
         
            debugMessenger = new VkDebugUtilsMessengerEXT(instance, SU.MakeDebugUtilsMessengerCreateInfoEXT());
            debugUtils = debugMessenger.DebugUtils;
 
            surfaceData = new SurfaceData(window, instance);

            physicalDevice = SU.PickPhysicalDevice(instance, surfaceData, deviceExtensions);
        }
                                       
        void CreateLogicalDevice()
        {
            QueueFamilyIndices indices = SU.FindGraphicsAndPresentQueueFamilyIndex(physicalDevice, surfaceData);

            PhysicalDeviceFeatures deviceFeatures = new()
            {
                SamplerAnisotropy = true
            };

            device = SU.CreateDevice(instance, physicalDevice, indices, deviceFeatures, validationLayers, deviceExtensions);
            
            graphicsQueue = device.GetQueue(indices.GraphicsFamily.Value);
            presentQueue = device.GetQueue(indices.PresentFamily.Value);          
        }
                                
        void CreateSwapChain()
        {
            QueueFamilyIndices indices = SU.FindGraphicsAndPresentQueueFamilyIndex(physicalDevice, surfaceData);

            swapChainData = new(
              physicalDevice,
              device,
              surfaceData,
              ImageUsageFlags.ImageUsageColorAttachmentBit | ImageUsageFlags.ImageUsageTransferSrcBit,
              new SwapchainKHR(null),
              indices.GraphicsFamily.Value,
              indices.PresentFamily.Value);
         
            swapChainExtent = swapChainData.SwapchainExtent;
        }
          
        ShaderModule CreateShaderModule(byte[] code)
        {
            fixed (byte* codePtr = code)
            {
                ShaderModuleCreateInfo createInfo = new(                                 
                    codeSize: (nuint)code.Length,
                    pCode: (uint*)codePtr
                );

                if (vk.CreateShaderModule(device, in createInfo, null, out ShaderModule shaderModule) != Result.Success)
                {
                    throw new Exception("failed to create shader module!");
                }

                return shaderModule;
            }

        }

        void CreateDescriptorSetLayout()
        {
            DescriptorSetLayoutBinding uboLayoutBinding = new()
            {                
                Binding = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ShaderStageVertexBit,
                PImmutableSamplers = null
            };

            DescriptorSetLayoutBinding samplerLayoutBinding = new()
            {
                Binding = 1,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.CombinedImageSampler,
                StageFlags = ShaderStageFlags.ShaderStageFragmentBit,
                PImmutableSamplers = null
            };

            DescriptorSetLayoutBinding* bindings = stackalloc DescriptorSetLayoutBinding[]
            { 
                uboLayoutBinding, 
                samplerLayoutBinding 
            };

            DescriptorSetLayoutCreateInfo layoutInfo = new()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = 2,
                PBindings = bindings
            };

            if (vk.CreateDescriptorSetLayout(device, in layoutInfo, null, out descriptorSetLayout) != Result.Success)
            {
                throw new Exception("failed to create descriptor set layout!");
            }
        }

        void CreateGraphicsPipeline()
        {
            var vertShaderCode = File.ReadAllBytes("vertshader.spv");
            var fragShaderCode = File.ReadAllBytes("fragshader.spv");

            ShaderModule vertShaderModule = CreateShaderModule(vertShaderCode);
            ShaderModule fragShaderModule = CreateShaderModule(fragShaderCode);

            PipelineShaderStageCreateInfo vertShaderStageInfo = new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.ShaderStageVertexBit,
                Module = vertShaderModule,
                PName = (byte*)SilkMarshal.StringToPtr("main")
            };

            PipelineShaderStageCreateInfo fragShaderStageInfo = new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.ShaderStageFragmentBit,
                Module = fragShaderModule,
                PName = (byte*)SilkMarshal.StringToPtr("main")
            };

            var shaderStages = stackalloc PipelineShaderStageCreateInfo[2];
            shaderStages[0] = vertShaderStageInfo;
            shaderStages[1] = fragShaderStageInfo;

            PipelineInputAssemblyStateCreateInfo inputAssembly = new()
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.TriangleList,
                PrimitiveRestartEnable = false
            };

            Viewport viewport = new()
            {
                X = 0.0f,
                Y = 0.0f,
                Width = swapChainExtent.Width,
                Height = swapChainExtent.Height,
                MinDepth = 0.0f,
                MaxDepth = 1.0f
            };

            Rect2D scissor = new()
            {
                Offset = new Offset2D(0, 0),
                Extent = swapChainExtent
            };

            PipelineViewportStateCreateInfo viewportState = new()
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                PViewports = &viewport,
                ScissorCount = 1,
                PScissors = &scissor
            };

            PipelineRasterizationStateCreateInfo rasterizer = new()
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                DepthClampEnable = false,
                RasterizerDiscardEnable = false,
                PolygonMode = PolygonMode.Fill,
                LineWidth = 1.0f,
                CullMode = CullModeFlags.CullModeBackBit,
                FrontFace = FrontFace.CounterClockwise,
                DepthBiasEnable = false,
                DepthBiasConstantFactor = 0.0f, // Optional
                DepthBiasClamp = 0.0f, // Optional
                DepthBiasSlopeFactor = 0.0f // Optional
            };

            PipelineMultisampleStateCreateInfo multisampling = new()
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                SampleShadingEnable = false,
                RasterizationSamples = SampleCountFlags.SampleCount1Bit,
                MinSampleShading = 1.0f, // Optional
                PSampleMask = null, // Optional
                AlphaToCoverageEnable = false, // Optional
                AlphaToOneEnable = false // Optional
            };

            PipelineColorBlendAttachmentState colorBlendAttachment = new()
            {
                ColorWriteMask = ColorComponentFlags.ColorComponentRBit |
                                 ColorComponentFlags.ColorComponentGBit |
                                 ColorComponentFlags.ColorComponentBBit |
                                 ColorComponentFlags.ColorComponentABit,
                BlendEnable = false,
                SrcColorBlendFactor = BlendFactor.One, // Optional
                DstColorBlendFactor = BlendFactor.Zero, // Optional
                ColorBlendOp = BlendOp.Add, // Optional
                SrcAlphaBlendFactor = BlendFactor.One, // Optional
                DstAlphaBlendFactor = BlendFactor.Zero, // Optional
                AlphaBlendOp = BlendOp.Add // Optional
            };

            PipelineColorBlendStateCreateInfo colorBlending = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOpEnable = false,
                LogicOp = LogicOp.Copy, // Optional
                AttachmentCount = 1,
                PAttachments = &colorBlendAttachment
            };
            colorBlending.BlendConstants[0] = 0.0f; // Optional
            colorBlending.BlendConstants[1] = 0.0f; // Optional
            colorBlending.BlendConstants[2] = 0.0f; // Optional
            colorBlending.BlendConstants[3] = 0.0f; // Optional

            fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &descriptorSetLayout)
            {
                PipelineLayoutCreateInfo pipelineLayoutInfo = new()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    SetLayoutCount = 1,
                    PSetLayouts = descriptorSetLayoutPtr,
                    PushConstantRangeCount = 0,
                    PPushConstantRanges = null
                };

                if (vk.CreatePipelineLayout(device, in pipelineLayoutInfo, null, out pipelineLayout) != Result.Success)
                {
                    throw new Exception("failed to create pipeline layout!");
                }
            }

            PipelineDepthStencilStateCreateInfo depthStencil = new()
            {
                SType = StructureType.PipelineDepthStencilStateCreateInfo,
                DepthTestEnable = true,
                DepthWriteEnable = true,
                DepthCompareOp = CompareOp.Less,
                DepthBoundsTestEnable = false,
                MinDepthBounds = 0,
                MaxDepthBounds = 1,
                StencilTestEnable = false,
            };

            var bindingDescription = Vertex.GetBindingDescription();
            var attributeDescriptions = Vertex.GetAttributeDescriptions();

            fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = &attributeDescriptions[0])
            {
                PipelineVertexInputStateCreateInfo vertexInputInfo = new()
                {
                    SType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexBindingDescriptionCount = 1,
                    PVertexBindingDescriptions = &bindingDescription, // Optional
                    VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                    PVertexAttributeDescriptions = attributeDescriptionsPtr // Optional
                };

                GraphicsPipelineCreateInfo pipelineInfo = new()
                {
                    SType = StructureType.GraphicsPipelineCreateInfo,
                    StageCount = 2,
                    PStages = shaderStages,
                    PVertexInputState = &vertexInputInfo,
                    PInputAssemblyState = &inputAssembly,
                    PViewportState = &viewportState,
                    PRasterizationState = &rasterizer,
                    PMultisampleState = &multisampling,
                    PDepthStencilState = &depthStencil,
                    PColorBlendState = &colorBlending,
                    PDynamicState = null, // Optional
                    Layout = pipelineLayout,
                    RenderPass = renderPass,
                    Subpass = 0,
                    BasePipelineHandle = new Pipeline(null), // Optional
                    BasePipelineIndex = -1, // Optional                   
                };

                if (vk.CreateGraphicsPipelines(device, new PipelineCache(null), 1, in pipelineInfo, null, out graphicsPipeline) != Result.Success)
                {
                    throw new Exception("failed to create graphics pipeline!");
                }
            }

            vk.DestroyShaderModule(device, fragShaderModule, null);
            vk.DestroyShaderModule(device, vertShaderModule, null);
        }

        void CreateRenderPass()
        {
            AttachmentDescription colorAttachment = new()
            {
                Format = swapChainData.ColorFormat,
                Samples = SampleCountFlags.SampleCount1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr,
            };

            AttachmentReference colorAttachmentRef = new()
            {
                Attachment = 0,
                Layout = ImageLayout.ColorAttachmentOptimal
            };

            AttachmentDescription depthAttachment = new()
            {
                Format = FindDepthFormat(),
                Samples = SampleCountFlags.SampleCount1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.DontCare,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            };

            AttachmentReference depthAttachmentRef = new()
            {
                Attachment = 1,
                Layout = ImageLayout.DepthStencilAttachmentOptimal
            };

            SubpassDescription subpass = new()
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorAttachmentRef,
                PDepthStencilAttachment = &depthAttachmentRef
            };


            SubpassDependency dependency = new()
            {
                SrcSubpass = Vk.SubpassExternal,
                DstSubpass = 0,
                SrcStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit |
                    PipelineStageFlags.PipelineStageEarlyFragmentTestsBit,
                SrcAccessMask = 0,
                DstStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit |
                    PipelineStageFlags.PipelineStageEarlyFragmentTestsBit,
                DstAccessMask = AccessFlags.AccessColorAttachmentReadBit |
                    AccessFlags.AccessColorAttachmentWriteBit |
                    AccessFlags.AccessDepthStencilAttachmentWriteBit
            };

            AttachmentDescription* attachments = stackalloc AttachmentDescription[2]
                {
                    colorAttachment,
                    depthAttachment
                };

            RenderPassCreateInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = 2,
                PAttachments = attachments,
                SubpassCount = 1,
                PSubpasses = &subpass,
                DependencyCount = 1,
                PDependencies = &dependency
            };

            if (vk.CreateRenderPass(device, in renderPassInfo, null, out renderPass) != Result.Success)
            {
                throw new Exception("failed to create render pass!");
            }

        }

        void CreateFramebuffers()
        {
            swapChainFramebuffers = new VkFramebuffer[swapChainData.ImageViews.Length];

            ImageView* attachments = stackalloc ImageView[2];
                   
            for (int i = 0; i < swapChainFramebuffers.Length; i++)
            {
                attachments[0] = swapChainData.ImageViews[i];
                attachments[1] = depthBuffer.imageView;

                FramebufferCreateInfo framebufferInfo = new(                                  
                    renderPass: renderPass,
                    attachmentCount: 2,
                    pAttachments: attachments,
                    width: swapChainExtent.Width,
                    height: swapChainExtent.Height,
                    layers: 1
                );

                swapChainFramebuffers[i] = new VkFramebuffer(device, framebufferInfo);                
            }
        }

        void CreateCommandPool()
        {
            QueueFamilyIndices queueFamilyIndices = SU.FindGraphicsAndPresentQueueFamilyIndex(physicalDevice, surfaceData);

            CommandPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                Flags = CommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit,
                QueueFamilyIndex = queueFamilyIndices.GraphicsFamily.Value
            };

            commandPool = new VkCommandPool(device, poolInfo);        
        }

        void CreateDepthResources()
        {
            Format depthFormat = FindDepthFormat();

            depthBuffer = new DepthBufferData(physicalDevice, device, depthFormat, swapChainExtent);
        
            SU.OneTimeSubmit(device, commandPool, graphicsQueue,
                commandBuffer => SU.SetImageLayout(commandBuffer, depthBuffer.image, depthFormat,
                ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal));
           
        }

        Format FindDepthFormat()
        {
            return FindSupportedFormat(new
                [] { Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint },
                ImageTiling.Optimal,
                FormatFeatureFlags.FormatFeatureDepthStencilAttachmentBit
                );
        }

        Format FindSupportedFormat(Format[] candidates, ImageTiling tiling, FormatFeatureFlags features)
        {
            foreach (Format format in candidates)
            {
                FormatProperties props = physicalDevice.GetFormatProperties(format);
             
                if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features)
                {
                    return format;
                }
                else if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features)
                {
                    return format;
                }
            }

            throw new Exception("failed to find supported format!");
        }

        bool HasStencilComponent(Format format)
        {
            return format == Format.D32SfloatS8Uint || format == Format.D24UnormS8Uint;
        }

        void CreateTextureImage()
        {
            var customConfig = SixLabors.ImageSharp.Configuration.Default.Clone();
            customConfig.PreferContiguousImageBuffers = true;

            using var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(customConfig, "texture.jpg");

            ulong imageSize = (ulong)(image.Width * image.Height * 4);
          
            if (!image.DangerousTryGetSinglePixelMemory(out Memory<SixLabors.ImageSharp.PixelFormats.Rgba32> memory))
            {
                throw new Exception("Error loading texture image");
            }

            textureData = new TextureData(physicalDevice, device,
              new Extent2D((uint)image.Width, (uint)image.Height),
              ImageUsageFlags.ImageUsageTransferDstBit | ImageUsageFlags.ImageUsageSampledBit,
              FormatFeatureFlags.FormatFeatureSampledImageBit, false, true);

            using var pinHandle = memory.Pin();
            void* srcData = pinHandle.Pointer;

            SU.OneTimeSubmit(device, commandPool, graphicsQueue,
                commandBuffer => textureData.SetImage(commandBuffer, srcData, imageSize));
        }
                                        
        void CreateVertexBuffer()
        {
            uint bufferSize = (uint)(Marshal.SizeOf(vertices[0]) * vertices.Length);

            vertexBuffer = new(physicalDevice, 
                device,
                bufferSize,
                BufferUsageFlags.BufferUsageTransferDstBit |
                    BufferUsageFlags.BufferUsageVertexBufferBit,
                MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);

            vertexBuffer.Upload<Vertex>(physicalDevice, device, commandPool, graphicsQueue, vertices);            
        }
                        
        uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
        {            
            for (int i = 0; i < physicalDevice.GetMemoryProperties().MemoryTypeCount; i++)
            {
                if ((typeFilter & (i << 1)) != 0 &&
                    (physicalDevice.GetMemoryProperties().MemoryTypes[i].PropertyFlags & properties) == properties)
                {
                    return (uint)i;
                }
            }

            throw new Exception("failed to find suitable memory type!");
        }

        void CreateIndexBuffer()
        {
            uint bufferSize = (uint)(Marshal.SizeOf(indices[0]) * indices.Length);

            indexBuffer = new(physicalDevice,
                device,
                bufferSize,
                BufferUsageFlags.BufferUsageTransferDstBit |
                    BufferUsageFlags.BufferUsageIndexBufferBit,
                MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);

            indexBuffer.Upload<ushort>(physicalDevice, device, commandPool, graphicsQueue, indices);
            
        }

        void CreateUniformBuffers()
        {
            uint bufferSize = (uint)sizeof(UniformBufferObject);

            uniformBuffers = new BufferData[MAX_FRAMES_IN_FLIGHT];
           
            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                uniformBuffers[i] = new BufferData(physicalDevice, device, bufferSize,
                    BufferUsageFlags.BufferUsageUniformBufferBit);           
            }
        }

        void CreateDescriptorPool()
        {
            int nrPools = 2;
            DescriptorPoolSize* poolSizes = stackalloc DescriptorPoolSize[nrPools];

            poolSizes[0] = new DescriptorPoolSize
            {
                Type = DescriptorType.UniformBuffer,
                DescriptorCount = MAX_FRAMES_IN_FLIGHT
            };

            poolSizes[1] = new DescriptorPoolSize
            {
                Type = DescriptorType.CombinedImageSampler,
                DescriptorCount = MAX_FRAMES_IN_FLIGHT
            };

            DescriptorPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint)nrPools,
                PPoolSizes = poolSizes,
                MaxSets = MAX_FRAMES_IN_FLIGHT
            };

            if (vk.CreateDescriptorPool(device, in poolInfo, null, out descriptorPool) != Result.Success)
            {
                throw new Exception("failed to create descriptor pool!");
            }

        }

        void CreateDescriptorSets()
        {
            DescriptorSetLayout* layouts = stackalloc DescriptorSetLayout[MAX_FRAMES_IN_FLIGHT];

            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                layouts[i] = descriptorSetLayout;
            }

            DescriptorSetAllocateInfo allocInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = descriptorPool,
                DescriptorSetCount = MAX_FRAMES_IN_FLIGHT,
                PSetLayouts = layouts
            };

            descriptorSets = new DescriptorSet[MAX_FRAMES_IN_FLIGHT];
            if (vk.AllocateDescriptorSets(device, &allocInfo, descriptorSets) != Result.Success)
            {
                throw new Exception("failed to allocate descriptor sets!");
            }

            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                DescriptorBufferInfo bufferInfo = new()
                {
                    Buffer = uniformBuffers[i].buffer,
                    Offset = 0,
                    Range = (ulong)sizeof(UniformBufferObject)
                };

                DescriptorImageInfo imageInfo = new();
                imageInfo.ImageLayout = ImageLayout.ShaderReadOnlyOptimal;
                imageInfo.ImageView = textureData.imageData.imageView;
                imageInfo.Sampler = textureData.sampler;

                WriteDescriptorSet[] descriptorWrites = new WriteDescriptorSet[2];

                descriptorWrites[0] = new WriteDescriptorSet
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = descriptorSets[i],
                    DstBinding = 0,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    PBufferInfo = &bufferInfo,
                };

                descriptorWrites[1] = new WriteDescriptorSet
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = descriptorSets[i],
                    DstBinding = 1,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 1,
                    PImageInfo = &imageInfo,

                };

                vk.UpdateDescriptorSets(
                    device,
                    (uint)descriptorWrites.Length,
                    new ReadOnlySpan<WriteDescriptorSet>(descriptorWrites),
                    0,
                    (CopyDescriptorSet*)null
                    );

            }
        }

        void CreateCommandBuffer()
        {
            CommandBufferAllocateInfo allocInfo = new(                          
                commandPool: commandPool,
                level: CommandBufferLevel.Primary,
                commandBufferCount: MAX_FRAMES_IN_FLIGHT
            );

            commandBuffers = new VkCommandBuffers(device, allocInfo).ToArray();            
        }

        void RecordCommandBuffer(VkCommandBuffer commandBuffer, uint imageIndex)
        {          
            commandBuffer.Begin(new(flags: 0, pInheritanceInfo: null));
         
            ClearValue* clearValues = stackalloc[]
                {
                    new ClearValue(new ClearColorValue(0, 0, 0, 1)),
                    new ClearValue(null, new ClearDepthStencilValue(1.0f, 0))
                };

            RenderPassBeginInfo renderPassInfo = new
                (                          
                    renderPass: renderPass,
                    framebuffer: swapChainFramebuffers[imageIndex],
                    clearValueCount: 2,
                    pClearValues: clearValues,
                    renderArea: new(new Offset2D(0, 0), swapChainExtent)
                );
       
            commandBuffer.BeginRenderPass(renderPassInfo, SubpassContents.Inline);

            commandBuffer.BindPipeline(PipelineBindPoint.Graphics, graphicsPipeline);
           
            commandBuffer.BindVertexBuffers(vertexBuffer.buffer, 0);

            commandBuffer.BindIndexBuffer(indexBuffer.buffer, 0, IndexType.Uint16);

            commandBuffer.BindDescriptorSets(PipelineBindPoint.Graphics, pipelineLayout,
                descriptorSets[currentFrame]);

            commandBuffer.DrawIndexed((uint)indices.Length, 1, 0, 0, 0);

            commandBuffer.EndRenderPass();

            commandBuffer.End(); 
        }

        void CreateSyncObjects()
        {
            imageAvailableSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
            renderFinishedSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
            inFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];

            SemaphoreCreateInfo semaphoreInfo = new()
            {
                SType = StructureType.SemaphoreCreateInfo
            };

            FenceCreateInfo fenceInfo = new()
            {
                SType = StructureType.FenceCreateInfo,
                Flags = FenceCreateFlags.FenceCreateSignaledBit
            };

            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                if ((vk.CreateSemaphore(device, semaphoreInfo, null, out imageAvailableSemaphores[i]) != Result.Success) ||
                    (vk.CreateSemaphore(device, semaphoreInfo, null, out renderFinishedSemaphores[i]) != Result.Success) ||
                    (vk.CreateFence(device, fenceInfo, null, out inFlightFences[i]) != Result.Success))
                {
                    throw new Exception("failed to create semaphores!");
                }
            }
        }

        void DrawFrame()
        {
            vk.WaitForFences(device, 1, inFlightFences[currentFrame], true, ulong.MaxValue);
           
            (uint imageIndex, Result result) = swapChainData.SwapChain.AquireNextImage(ulong.MaxValue,
                imageAvailableSemaphores[currentFrame], new Fence(null));

            if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || isFramebufferResized)
            {
                isFramebufferResized = false;
                RecreateSwapChain();
                return;
            }
            else if (result != Result.Success)
            {
                throw new Exception("failed to acquire swap chain image!");
            }

            vk.ResetFences(device, 1, inFlightFences[currentFrame]);

            commandBuffers[currentFrame].Reset(0);

            RecordCommandBuffer(commandBuffers[currentFrame], imageIndex);

            UpdateUniformBuffer(imageIndex);

            Semaphore* waitSemaphores = stackalloc[] { imageAvailableSemaphores[currentFrame] };
            PipelineStageFlags* waitStages = stackalloc[] { PipelineStageFlags.PipelineStageColorAttachmentOutputBit };
            Semaphore signalSemaphore = renderFinishedSemaphores[currentFrame];
            
            CommandBuffer commandBuffer = commandBuffers[currentFrame];

            SubmitInfo submitInfo = new(                           
                waitSemaphoreCount: 1,
                pWaitSemaphores: waitSemaphores,
                pWaitDstStageMask: waitStages,
                commandBufferCount: 1,
                pCommandBuffers: &commandBuffer,
                signalSemaphoreCount: 1,
                pSignalSemaphores: &signalSemaphore
            );

            graphicsQueue.Submit(submitInfo, inFlightFences[currentFrame]);
            
            currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
            
            SwapchainKHR swapChain = swapChainData.SwapChain;

            PresentInfoKHR presentInfo = new(          
                waitSemaphoreCount: 1,
                pWaitSemaphores: &signalSemaphore,
                swapchainCount: 1,
                pSwapchains: &swapChain,
                pImageIndices: &imageIndex,
                pResults: null
            );

            presentQueue.PresentKHR(swapChainData.SwapChain, presentInfo);            
        }

        static readonly long startTime = DateTime.Now.Ticks;

        void UpdateUniformBuffer(uint currentImage)
        {
            long currentTime = DateTime.Now.Ticks;
            float time = (float)TimeSpan.FromTicks(currentTime - startTime).TotalSeconds;

            UniformBufferObject ubo = new();

            ubo.model = Matrix4X4.CreateFromAxisAngle(
                new Vector3D<float>(0, 0, 1),
                time * Scalar.DegreesToRadians(90.0f));

            ubo.view = Matrix4X4.CreateLookAt(
                new Vector3D<float>(2.0f, 2.0f, 2.0f),
                new Vector3D<float>(0.0f, 0.0f, 0.0f),
                new Vector3D<float>(0.0f, 0.0f, -0.1f));

            ubo.proj = Matrix4X4.CreatePerspectiveFieldOfView(
                Scalar.DegreesToRadians(45.0f),
                swapChainExtent.Width / (float)swapChainExtent.Height,
                0.1f,
                10.0f);

            ubo.proj.M11 *= -1;

            uniformBuffers[currentFrame].Upload(ubo);
          
        }

        void RecreateSwapChain()
        {
            while (window.Size.X == 0 || window.Size.Y == 0)
            {
                window.DoEvents();
            }

            device.WaitIdle();

            CleanupSwapChain();

            CreateSwapChain();         
            CreateRenderPass();
            CreateGraphicsPipeline();
            CreateDepthResources();
            CreateFramebuffers();
        }

        void CleanupSwapChain()
        {
            depthBuffer.Dispose();
         
            foreach (var framebuffer in swapChainFramebuffers)
            {
                framebuffer.Dispose();              
            }

            vk.DestroyPipeline(device, graphicsPipeline, null);
            vk.DestroyPipelineLayout(device, pipelineLayout, null);
            vk.DestroyRenderPass(device, renderPass, null);

            swapChainData.Clear(device);      
        }

        void Cleanup()
        {
            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                vk.DestroySemaphore(device, imageAvailableSemaphores[i], null);
                vk.DestroySemaphore(device, renderFinishedSemaphores[i], null);
                vk.DestroyFence(device, inFlightFences[i], null);
            }

            commandPool.Dispose();
         
            CleanupSwapChain();

            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                uniformBuffers[i].Dispose();     
            }

            textureData.Dispose();

            vk.DestroyDescriptorPool(device, descriptorPool, null);
            vk.DestroyDescriptorSetLayout(device, descriptorSetLayout, null);

            indexBuffer.Dispose();      
            vertexBuffer.Dispose();
        
            device.Dispose();

            surfaceData.Dispose();
            debugMessenger.Dispose();
            instance.Dispose();
   
            window.Close();
            window.Dispose();

        }

    }

}
