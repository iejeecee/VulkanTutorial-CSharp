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
        static readonly long startTime = DateTime.Now.Ticks;

        const int MAX_FRAMES_IN_FLIGHT = 2;

        uint currentFrame = 0;
        bool isFramebufferResized = false;

        IWindow window;
        //Vk vk;     

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

        VkRenderPass renderPass;
        VkDescriptorSetLayout descriptorSetLayout;

        VkPipelineLayout pipelineLayout;
        VkPipeline graphicsPipeline;

        VkSemaphore[] imageAvailableSemaphores;
        VkSemaphore[] renderFinishedSemaphores;
        VkFence[] inFlightFences;

        BufferData vertexBuffer;
        BufferData indexBuffer;
        BufferData[] uniformBuffers;

        TextureData textureData;
    
        DepthBufferData depthBuffer;
    
        VkDescriptorPool descriptorPool;
        VkDescriptorSet[] descriptorSets;

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
            //vk = Vk.GetApi();

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
          
        VkShaderModule CreateShaderModule(string filename)
        {
            byte[] code = File.ReadAllBytes(filename);

            fixed (byte* codePtr = code)
            {
                ShaderModuleCreateInfo createInfo = new(                                 
                    codeSize: (nuint)code.Length,
                    pCode: (uint*)codePtr
                );

                return new VkShaderModule(device, createInfo);             
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

            DescriptorSetLayoutCreateInfo layoutInfo = new
            (             
                bindingCount: 2,
                pBindings: bindings
            );

            descriptorSetLayout = new VkDescriptorSetLayout(device, layoutInfo);        
        }

        void CreateGraphicsPipeline()
        {        
            VkShaderModule vertShaderModule = CreateShaderModule("vertshader.spv");
            VkShaderModule fragShaderModule = CreateShaderModule("fragshader.spv");

            PipelineShaderStageCreateInfo vertShaderStageInfo = new
            (               
                stage: ShaderStageFlags.ShaderStageVertexBit,
                module: vertShaderModule,
                pName: (byte*)SilkMarshal.StringToPtr("main")
            );

            PipelineShaderStageCreateInfo fragShaderStageInfo = new
            (               
                stage: ShaderStageFlags.ShaderStageFragmentBit,
                module: fragShaderModule,
                pName: (byte*)SilkMarshal.StringToPtr("main")
            );

            var shaderStages = stackalloc PipelineShaderStageCreateInfo[]
            {
                vertShaderStageInfo,
                fragShaderStageInfo
            };

            PipelineInputAssemblyStateCreateInfo inputAssembly = new
            (               
                topology: PrimitiveTopology.TriangleList,
                primitiveRestartEnable: false
            );

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

            PipelineViewportStateCreateInfo viewportState = new
            (               
                viewportCount: 1,
                pViewports: &viewport,
                scissorCount: 1,
                pScissors: &scissor
            );

            PipelineRasterizationStateCreateInfo rasterizer = new
            (             
                depthClampEnable: false,
                rasterizerDiscardEnable: false,
                polygonMode: PolygonMode.Fill,
                lineWidth: 1.0f,
                cullMode: CullModeFlags.CullModeBackBit,
                frontFace: FrontFace.CounterClockwise,
                depthBiasEnable: false,
                depthBiasConstantFactor: 0.0f, 
                depthBiasClamp: 0.0f, 
                depthBiasSlopeFactor: 0.0f 
            );

            PipelineMultisampleStateCreateInfo multisampling = new
            (               
                sampleShadingEnable: false,
                rasterizationSamples: SampleCountFlags.SampleCount1Bit,
                minSampleShading: 1.0f, 
                pSampleMask: null, 
                alphaToCoverageEnable: false, 
                alphaToOneEnable: false 
            );

            PipelineColorBlendAttachmentState colorBlendAttachment = new()
            {
                ColorWriteMask = ColorComponentFlags.ColorComponentRBit |
                                 ColorComponentFlags.ColorComponentGBit |
                                 ColorComponentFlags.ColorComponentBBit |
                                 ColorComponentFlags.ColorComponentABit,
                BlendEnable = false,
                SrcColorBlendFactor = BlendFactor.One, 
                DstColorBlendFactor = BlendFactor.Zero, 
                ColorBlendOp = BlendOp.Add, 
                SrcAlphaBlendFactor = BlendFactor.One, 
                DstAlphaBlendFactor = BlendFactor.Zero, 
                AlphaBlendOp = BlendOp.Add 
            };

            PipelineColorBlendStateCreateInfo colorBlending = new
            (               
                logicOpEnable: false,
                logicOp: LogicOp.Copy, 
                attachmentCount: 1,
                pAttachments: &colorBlendAttachment                           
            );
        
            colorBlending.BlendConstants[0] = 0.0f; 
            colorBlending.BlendConstants[1] = 0.0f; 
            colorBlending.BlendConstants[2] = 0.0f; 
            colorBlending.BlendConstants[3] = 0.0f;

            DescriptorSetLayout descriptorSetLayoutPtr = descriptorSetLayout;
            
            PipelineLayoutCreateInfo pipelineLayoutInfo = new
            (                    
                setLayoutCount: 1,
                pSetLayouts: &descriptorSetLayoutPtr,
                pushConstantRangeCount: 0,
                pPushConstantRanges: null
            );

            pipelineLayout = new VkPipelineLayout(device, pipelineLayoutInfo);
                            
            PipelineDepthStencilStateCreateInfo depthStencil = new
            (              
                depthTestEnable: true,
                depthWriteEnable: true,
                depthCompareOp: CompareOp.Less,
                depthBoundsTestEnable: false,
                minDepthBounds: 0,
                maxDepthBounds: 1,
                stencilTestEnable: false
            );

            var bindingDescription = Vertex.GetBindingDescription();
            var attributeDescriptions = Vertex.GetAttributeDescriptions();

            fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = &attributeDescriptions[0])
            {
                PipelineVertexInputStateCreateInfo vertexInputInfo = new
                (                    
                    vertexBindingDescriptionCount: 1,
                    pVertexBindingDescriptions: &bindingDescription, 
                    vertexAttributeDescriptionCount: (uint)attributeDescriptions.Length,
                    pVertexAttributeDescriptions: attributeDescriptionsPtr 
                );

                GraphicsPipelineCreateInfo pipelineInfo = new
                (                   
                    stageCount: 2,
                    pStages: shaderStages,
                    pVertexInputState: &vertexInputInfo,
                    pInputAssemblyState: &inputAssembly,
                    pViewportState: &viewportState,
                    pRasterizationState: &rasterizer,
                    pMultisampleState: &multisampling,
                    pDepthStencilState: &depthStencil,
                    pColorBlendState: &colorBlending,
                    pDynamicState: null, 
                    layout: pipelineLayout,
                    renderPass: renderPass,
                    subpass: 0,
                    basePipelineHandle: new Pipeline(null), 
                    basePipelineIndex: -1                    
                );

                graphicsPipeline = new VkPipeline(device, new PipelineCache(null), pipelineInfo);               
            }

            fragShaderModule.Dispose();
            vertShaderModule.Dispose();
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

            AttachmentDescription* attachments = stackalloc AttachmentDescription[]
            {
                colorAttachment,
                depthAttachment
            };

            RenderPassCreateInfo renderPassInfo = new
            (               
                attachmentCount: 2,
                pAttachments: attachments,
                subpassCount: 1,
                pSubpasses: &subpass,
                dependencyCount: 1,
                pDependencies: &dependency
            );

            renderPass = new VkRenderPass(device, renderPassInfo);
          

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

            CommandPoolCreateInfo poolInfo = new
            (             
                flags: CommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit,
                queueFamilyIndex: queueFamilyIndices.GraphicsFamily.Value
            );

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

            textureData = new TextureData(physicalDevice,
                device,
                new Extent2D((uint)image.Width, (uint)image.Height),
                ImageUsageFlags.ImageUsageTransferDstBit | ImageUsageFlags.ImageUsageSampledBit,
                FormatFeatureFlags.FormatFeatureSampledImageBit, 
                false, 
                true);

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
            DescriptorPoolSize* poolSizes = stackalloc DescriptorPoolSize[]
            {
                new DescriptorPoolSize
                {
                    Type = DescriptorType.UniformBuffer,
                    DescriptorCount = MAX_FRAMES_IN_FLIGHT
                },

                new DescriptorPoolSize
                {
                    Type = DescriptorType.CombinedImageSampler,
                    DescriptorCount = MAX_FRAMES_IN_FLIGHT
                }
            };

            DescriptorPoolCreateInfo poolInfo = new
            (               
                poolSizeCount: 2,
                pPoolSizes: poolSizes,
                maxSets: MAX_FRAMES_IN_FLIGHT
            );

            descriptorPool = new VkDescriptorPool(device, poolInfo);          
        }

        void CreateDescriptorSets()
        {
            DescriptorSetLayout* layouts = stackalloc DescriptorSetLayout[MAX_FRAMES_IN_FLIGHT];

            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                layouts[i] = descriptorSetLayout;
            }

            DescriptorSetAllocateInfo allocInfo = new
            (
                descriptorPool: descriptorPool,
                descriptorSetCount: MAX_FRAMES_IN_FLIGHT,
                pSetLayouts: layouts
            );

            descriptorSets = new VkDescriptorSets(device, allocInfo).ToArray();
          
            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                DescriptorBufferInfo bufferInfo = new()
                {
                    Buffer = uniformBuffers[i].buffer,
                    Offset = 0,
                    Range = (ulong)sizeof(UniformBufferObject)
                };

                DescriptorImageInfo imageInfo = new()
                {
                    ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                    ImageView = textureData.imageData.imageView,
                    Sampler = textureData.sampler
                };

                WriteDescriptorSet[] descriptorWrites = new WriteDescriptorSet[]
                {
                    new WriteDescriptorSet
                    (
                        dstSet: descriptorSets[i],
                        dstBinding: 0,
                        dstArrayElement: 0,
                        descriptorType: DescriptorType.UniformBuffer,
                        descriptorCount: 1,
                        pBufferInfo: &bufferInfo
                    ),

                    new WriteDescriptorSet
                    (
                        dstSet: descriptorSets[i],
                        dstBinding: 1,
                        dstArrayElement: 0,
                        descriptorType: DescriptorType.CombinedImageSampler,
                        descriptorCount: 1,
                        pImageInfo: &imageInfo
                    )
                };

                device.UpdateDescriptorSets(descriptorWrites, null);                
            }
        }

        void CreateCommandBuffer()
        {
            CommandBufferAllocateInfo allocInfo = new
            (                          
                commandPool: commandPool,
                level: CommandBufferLevel.Primary,
                commandBufferCount: MAX_FRAMES_IN_FLIGHT
            );

            commandBuffers = new VkCommandBuffers(device, allocInfo).ToArray();            
        }

        void RecordCommandBuffer(VkCommandBuffer commandBuffer, uint imageIndex)
        {          
            commandBuffer.Begin(new(flags:0));
         
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
            imageAvailableSemaphores = new VkSemaphore[MAX_FRAMES_IN_FLIGHT];
            renderFinishedSemaphores = new VkSemaphore[MAX_FRAMES_IN_FLIGHT];
            inFlightFences = new VkFence[MAX_FRAMES_IN_FLIGHT];
        
            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                imageAvailableSemaphores[i] = new VkSemaphore(device, new(flags: 0));
                renderFinishedSemaphores[i] = new VkSemaphore(device, new(flags: 0));
                inFlightFences[i] = new VkFence(device, new(flags: FenceCreateFlags.FenceCreateSignaledBit));             
            }
        }

        void DrawFrame()
        {
            device.WaitForFences(inFlightFences[currentFrame], ulong.MaxValue);
                    
            (uint imageIndex, Result result) = swapChainData.SwapChain.AquireNextImage(ulong.MaxValue,
                imageAvailableSemaphores[currentFrame], null);

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

            device.ResetFences(inFlightFences[currentFrame]);

            commandBuffers[currentFrame].Reset(0);

            RecordCommandBuffer(commandBuffers[currentFrame], imageIndex);

            UpdateUniformBuffer(imageIndex);

            Semaphore waitSemaphore = imageAvailableSemaphores[currentFrame];
            PipelineStageFlags waitStages = PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
            Semaphore signalSemaphore = renderFinishedSemaphores[currentFrame];
            
            CommandBuffer commandBuffer = commandBuffers[currentFrame];

            SubmitInfo submitInfo = new
            (                           
                waitSemaphoreCount: 1,
                pWaitSemaphores: &waitSemaphore,
                pWaitDstStageMask: &waitStages,
                commandBufferCount: 1,
                pCommandBuffers: &commandBuffer,
                signalSemaphoreCount: 1,
                pSignalSemaphores: &signalSemaphore
            );

            graphicsQueue.Submit(submitInfo, inFlightFences[currentFrame]);
            
            currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
            
            SwapchainKHR swapChain = swapChainData.SwapChain;

            PresentInfoKHR presentInfo = new
            (                 
                waitSemaphoreCount: 1,
                pWaitSemaphores: &signalSemaphore,
                swapchainCount: 1,
                pSwapchains: &swapChain,
                pImageIndices: &imageIndex,
                pResults: null
            );

            presentQueue.PresentKHR(swapChainData.SwapChain, presentInfo);            
        }
        
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

            graphicsPipeline.Dispose();
            pipelineLayout.Dispose();       
            renderPass.Dispose();

            swapChainData.Clear(device);      
        }

        void Cleanup()
        {
            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                imageAvailableSemaphores[i].Dispose();
                renderFinishedSemaphores[i].Dispose();
                inFlightFences[i].Dispose();             
            }

            commandPool.Dispose();
         
            CleanupSwapChain();

            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                uniformBuffers[i].Dispose();     
            }

            textureData.Dispose();

            descriptorPool.Dispose();
            descriptorSetLayout.Dispose();

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
