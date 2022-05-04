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
using VulkanTest.VkOO;

namespace VulkanTest
{

    unsafe class VulkanTutorial
    {

        const int MAX_FRAMES_IN_FLIGHT = 2;

        uint currentFrame = 0;
        bool isFramebufferResized = false;

        IWindow window;
        Vk vk;
        Instance instance;
        VkInstance vkInstance;
        VkSurfaceData surfaceData;

        SurfaceKHR surface;
        SwapchainKHR swapChain;
      
        KhrSwapchain vkSwapChain;

        VkPhysicalDevice physicalDevice;
        VkDevice vkDevice;
        Device device;

        Queue graphicsQueue;
        Queue presentQueue;

        Image[] swapChainImages;
        Format swapChainImageFormat;
        Extent2D swapChainExtent;

        ImageView[] swapChainImageViews;
        Framebuffer[] swapChainFramebuffers;

        CommandPool commandPool;
        CommandBuffer[] commandBuffers;

        RenderPass renderPass;
        DescriptorSetLayout descriptorSetLayout;
        PipelineLayout pipelineLayout;

        Pipeline graphicsPipeline;

        Semaphore[] imageAvailableSemaphores;
        Semaphore[] renderFinishedSemaphores;
        Fence[] inFlightFences;

        Silk.NET.Vulkan.Buffer vertexBuffer;
        DeviceMemory vertexBufferMemory;

        Silk.NET.Vulkan.Buffer indexBuffer;
        DeviceMemory indexBufferMemory;

        Silk.NET.Vulkan.Buffer[] uniformBuffers;
        DeviceMemory[] uniformBuffersMemory;

        Image textureImage;
        DeviceMemory textureImageMemory;
        ImageView textureImageView;
        Sampler textureSampler;

        Image depthImage;
        DeviceMemory depthImageMemory;
        ImageView depthImageView;

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
            VkSurfaceData.CreateWindow("hallo", new Extent2D(800, 600), out window,
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

            vk.DeviceWaitIdle(device);

            Cleanup();
        }

        private void Window_Render(double obj)
        {
            DrawFrame();
        }

        public void InitializeVulkan()
        {
            CreateInstance();        
            PickPhysicalDevice();
            CreateLogicalDevice();

            CreateSwapChain();
            CreateImageViews();
            CreateRenderPass();
            CreateDescriptorSetLayout();
            CreateGraphicsPipeline();
            CreateCommandPool();

            CreateDepthResources();
            CreateFramebuffers();
            CreateTextureImage();
            CreateTextureImageView();
            CreateTextureSampler();
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
            var extensions = requiredExtensions.Concat(instanceExtensions).ToArray();

            vkInstance = new VkInstance("textureDemo", "none", Vk.Version11,
                extensions, validationLayers);

            vk = vkInstance.Vk;
            instance = vkInstance.Instance;

            surfaceData = new VkSurfaceData(window, vkInstance);
          
            surface = surfaceData.Surface;            
        }
                        
        void PickPhysicalDevice()
        {
            VkPhysicalDevice[] devices = vkInstance.EnumerateDevices();
            
            foreach (var device in devices)
            {
                if (IsDeviceSuitable(device))
                {
                    physicalDevice = device;
                    break;
                }
            }

            if (physicalDevice == null)
            {
                throw new Exception("failed to find a suitable GPU!");
            }

        }

        bool IsDeviceSuitable(VkPhysicalDevice device)
        {
            bool isExtensionsSupported = device.CheckExtensionsSupported(deviceExtensions);

            var indices = device.FindGraphicsAndPresentQueueFamilyIndex(surfaceData);

            bool isSwapChainAdequate = false;
            if (isExtensionsSupported)
            {
                SwapChainSupportDetails swapChainSupport = device.QuerySwapChainSupport(surfaceData);
                isSwapChainAdequate = (swapChainSupport.Formats != null) && (swapChainSupport.PresentModes != null);
            }

            return indices.IsComplete && isExtensionsSupported && isSwapChainAdequate
                && device.SupportedFeatures.SamplerAnisotropy;
        }

        void CreateLogicalDevice()
        {
            QueueFamilyIndices indices = physicalDevice.FindGraphicsAndPresentQueueFamilyIndex(surfaceData);

            PhysicalDeviceFeatures deviceFeatures = new()
            {
                SamplerAnisotropy = true
            };

            vkDevice = new VkDevice(physicalDevice, indices, deviceFeatures, validationLayers, deviceExtensions);

            device = vkDevice.Device;

            graphicsQueue = vkDevice.GetDeviceQueue(indices.GraphicsFamily.Value);
            presentQueue = vkDevice.GetDeviceQueue(indices.PresentFamily.Value);
          
        }
        
        SurfaceFormatKHR ChooseSwapSurfaceFormat(SurfaceFormatKHR[] availableFormats)
        {
            foreach (var availableFormat in availableFormats)
            {
                if (availableFormat.Format == Format.B8G8R8A8Srgb &&
                    availableFormat.ColorSpace == ColorSpaceKHR.ColorSpaceSrgbNonlinearKhr)
                {
                    return availableFormat;
                }
            }

            return availableFormats[0];
        }

        PresentModeKHR ChooseSwapPresentMode(PresentModeKHR[] availablePresentModes)
        {
            foreach (var availablePresentMode in availablePresentModes)
            {
                if (availablePresentMode == PresentModeKHR.PresentModeMailboxKhr)
                {
                    return availablePresentMode;
                }
            }

            return PresentModeKHR.PresentModeFifoKhr;
        }

        Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
        {
            if (capabilities.CurrentExtent.Width != uint.MaxValue)
            {
                return capabilities.CurrentExtent;
            }
            else
            {
                Extent2D actualExtent = new()
                {
                    Width = (uint)window.FramebufferSize.X,
                    Height = (uint)window.FramebufferSize.Y
                };

                actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
                actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

                return actualExtent;
            }
        }

        void CreateSwapChain()
        {
            SwapChainSupportDetails swapChainSupport = physicalDevice.QuerySwapChainSupport(surfaceData);

            SurfaceFormatKHR surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
            PresentModeKHR presentMode = ChooseSwapPresentMode(swapChainSupport.PresentModes);
            Extent2D extent = ChooseSwapExtent(swapChainSupport.Capabilities);

            uint imageCount = swapChainSupport.Capabilities.MinImageCount + 1;

            if (swapChainSupport.Capabilities.MaxImageCount > 0 &&
                imageCount > swapChainSupport.Capabilities.MaxImageCount)
            {
                imageCount = swapChainSupport.Capabilities.MaxImageCount;
            }

            SwapchainCreateInfoKHR createInfo = new()
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                Surface = surface,
                MinImageCount = imageCount,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageExtent = extent,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ImageUsageColorAttachmentBit
            };

            QueueFamilyIndices indices = physicalDevice.FindGraphicsAndPresentQueueFamilyIndex(surfaceData);

            uint* queueFamilyIndices = stackalloc uint[2];

            queueFamilyIndices[0] = indices.GraphicsFamily.Value;
            queueFamilyIndices[1] = indices.PresentFamily.Value;

            if (indices.GraphicsFamily != indices.PresentFamily)
            {
                createInfo.ImageSharingMode = SharingMode.Concurrent;
                createInfo.QueueFamilyIndexCount = 2;
                createInfo.PQueueFamilyIndices = queueFamilyIndices;
            }
            else
            {
                createInfo.ImageSharingMode = SharingMode.Exclusive;
                createInfo.QueueFamilyIndexCount = 0; // Optional
                createInfo.PQueueFamilyIndices = null; // Optional
            }

            createInfo.PreTransform = swapChainSupport.Capabilities.CurrentTransform;
            createInfo.CompositeAlpha = CompositeAlphaFlagsKHR.CompositeAlphaOpaqueBitKhr;
            createInfo.PresentMode = presentMode;
            createInfo.Clipped = new Bool32(true);
            createInfo.OldSwapchain = new SwapchainKHR();

            if (!vk.TryGetDeviceExtension(instance, vk.CurrentDevice.Value, out vkSwapChain))
            {
                throw new NotSupportedException("KHR_swapchain extension not found.");
            }

            if (vkSwapChain.CreateSwapchain(device, in createInfo, null, out swapChain) != Result.Success)
            {
                throw new Exception("failed to create swap chain!");
            }

            vkSwapChain.GetSwapchainImages(device, swapChain, &imageCount, null);
            swapChainImages = new Image[imageCount];
            vkSwapChain.GetSwapchainImages(device, swapChain, &imageCount, swapChainImages);

            swapChainImageFormat = surfaceFormat.Format;
            swapChainExtent = extent;
        }

        void CreateImageViews()
        {
            swapChainImageViews = new ImageView[swapChainImages.Length];

            for (int i = 0; i < swapChainImages.Length; i++)
            {
                swapChainImageViews[i] = CreateImageView(swapChainImages[i], swapChainImageFormat,
                    ImageAspectFlags.ImageAspectColorBit);
            }
        }

        byte[] LoadShader(string filename)
        {
            return File.ReadAllBytes(filename);
        }

        ShaderModule CreateShaderModule(byte[] code)
        {
            fixed (byte* codePtr = code)
            {
                ShaderModuleCreateInfo createInfo = new()
                {
                    SType = StructureType.ShaderModuleCreateInfo,
                    CodeSize = (nuint)code.Length,
                    PCode = (uint*)codePtr
                };

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
            var vertShaderCode = LoadShader("vertshader.spv");
            var fragShaderCode = LoadShader("fragshader.spv");

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
                Format = swapChainImageFormat,
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
            swapChainFramebuffers = new Framebuffer[swapChainImageViews.Length];

            for (int i = 0; i < swapChainFramebuffers.Length; i++)
            {
                ImageView* attachments = stackalloc[]
                    {
                        swapChainImageViews[i],
                        depthImageView
                    };

                FramebufferCreateInfo framebufferInfo = new()
                {
                    SType = StructureType.FramebufferCreateInfo,
                    RenderPass = renderPass,
                    AttachmentCount = 2,
                    PAttachments = attachments,
                    Width = swapChainExtent.Width,
                    Height = swapChainExtent.Height,
                    Layers = 1
                };

                if (vk.CreateFramebuffer(device, in framebufferInfo, null, out swapChainFramebuffers[i]) != Result.Success)
                {
                    throw new Exception("failed to create framebuffer!");
                }

            }
        }

        void CreateCommandPool()
        {
            QueueFamilyIndices queueFamilyIndices = physicalDevice.FindGraphicsAndPresentQueueFamilyIndex(surfaceData);

            CommandPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                Flags = CommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit,
                QueueFamilyIndex = queueFamilyIndices.GraphicsFamily.Value
            };

            if (vk.CreateCommandPool(device, in poolInfo, null, out commandPool) != Result.Success)
            {
                throw new Exception("failed to create command pool!");
            }

        }

        void CreateDepthResources()
        {
            Format depthFormat = FindDepthFormat();

            CreateImage(swapChainExtent.Width, swapChainExtent.Height, depthFormat,
                ImageTiling.Optimal, ImageUsageFlags.ImageUsageDepthStencilAttachmentBit,
                MemoryPropertyFlags.MemoryPropertyDeviceLocalBit,
                out depthImage, out depthImageMemory);

            depthImageView = CreateImageView(depthImage, depthFormat,
                ImageAspectFlags.ImageAspectDepthBit);

            TransitionImageLayout(depthImage, depthFormat, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);

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

            CreateBuffer(imageSize, BufferUsageFlags.BufferUsageTransferSrcBit,
                MemoryPropertyFlags.MemoryPropertyHostVisibleBit |
                MemoryPropertyFlags.MemoryPropertyHostCoherentBit,
                out Silk.NET.Vulkan.Buffer stagingBuffer,
                out DeviceMemory stagingBufferMemory);

            if (!image.DangerousTryGetSinglePixelMemory(out Memory<SixLabors.ImageSharp.PixelFormats.Rgba32> memory))
            {
                throw new Exception("Error loading texture image");
            }

            using (var pinHandle = memory.Pin())
            {
                void* srcData = pinHandle.Pointer;

                void* data;
                vk.MapMemory(device, stagingBufferMemory, 0, imageSize, 0, &data);
                System.Buffer.MemoryCopy(srcData, data, imageSize, imageSize);
                vk.UnmapMemory(device, stagingBufferMemory);
            }

            CreateImage((uint)image.Width, (uint)image.Height,
                Format.R8G8B8A8Srgb, ImageTiling.Optimal,
                ImageUsageFlags.ImageUsageTransferDstBit | ImageUsageFlags.ImageUsageSampledBit,
                MemoryPropertyFlags.MemoryPropertyDeviceLocalBit, out textureImage, out textureImageMemory);

            TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
            CopyBufferToImage(stagingBuffer, textureImage, (uint)image.Width, (uint)image.Height);
            TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

            vk.DestroyBuffer(device, stagingBuffer, null);
            vk.FreeMemory(device, stagingBufferMemory, null);
        }

        void CreateImage(
            uint width,
            uint height,
            Format format,
            ImageTiling tiling,
            ImageUsageFlags usage,
            MemoryPropertyFlags properties,
            out Image image,
            out DeviceMemory imageMemory
            )
        {

            ImageCreateInfo imageInfo = new()
            {
                SType = StructureType.ImageCreateInfo,
                ImageType = ImageType.ImageType2D,
                MipLevels = 1,
                ArrayLayers = 1,
                Format = format,
                Tiling = tiling,
                InitialLayout = ImageLayout.Undefined,
                Usage = usage,
                SharingMode = SharingMode.Exclusive,
                Samples = SampleCountFlags.SampleCount1Bit
            };

            imageInfo.Extent.Width = width;
            imageInfo.Extent.Height = height;
            imageInfo.Extent.Depth = 1;

            if (vk.CreateImage(device, in imageInfo, null, out image) != Result.Success)
            {
                throw new Exception("failed to create image!");
            }

            vk.GetImageMemoryRequirements(device, image,
                out MemoryRequirements memRequirements);

            MemoryAllocateInfo allocInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits,
                properties)
            };

            if (vk.AllocateMemory(device, in allocInfo, null, out imageMemory) != Result.Success)
            {
                throw new Exception("failed to allocate image memory!");
            }

            vk.BindImageMemory(device, image, imageMemory, 0);
        }

        void TransitionImageLayout(
            Image image,
            Format format,
            ImageLayout oldLayout,
            ImageLayout newLayout
            )
        {

            CommandBuffer commandBuffer = BeginSingleTimeCommands();

            ImageMemoryBarrier barrier = new();

            barrier.SType = StructureType.ImageMemoryBarrier;
            barrier.OldLayout = oldLayout;
            barrier.NewLayout = newLayout;
            barrier.SrcQueueFamilyIndex = Vk.QueueFamilyIgnored;
            barrier.DstQueueFamilyIndex = Vk.QueueFamilyIgnored;
            barrier.Image = image;
            barrier.SubresourceRange.BaseMipLevel = 0;
            barrier.SubresourceRange.LevelCount = 1;
            barrier.SubresourceRange.BaseArrayLayer = 0;
            barrier.SubresourceRange.LayerCount = 1;

            if (newLayout == ImageLayout.DepthStencilAttachmentOptimal)
            {
                barrier.SubresourceRange.AspectMask = ImageAspectFlags.ImageAspectDepthBit;

                if (HasStencilComponent(format))
                {
                    barrier.SubresourceRange.AspectMask |= ImageAspectFlags.ImageAspectStencilBit;
                }
            }
            else
            {
                barrier.SubresourceRange.AspectMask = ImageAspectFlags.ImageAspectColorBit; ;
            }

            PipelineStageFlags sourceStage;
            PipelineStageFlags destinationStage;

            if (oldLayout == ImageLayout.Undefined &&
                newLayout == ImageLayout.TransferDstOptimal)
            {
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.AccessTransferWriteBit;

                sourceStage = PipelineStageFlags.PipelineStageTopOfPipeBit;
                destinationStage = PipelineStageFlags.PipelineStageTransferBit;
            }
            else if (oldLayout == ImageLayout.TransferDstOptimal &&
                newLayout == ImageLayout.ShaderReadOnlyOptimal)
            {
                barrier.SrcAccessMask = AccessFlags.AccessTransferWriteBit;
                barrier.DstAccessMask = AccessFlags.AccessShaderReadBit;

                sourceStage = PipelineStageFlags.PipelineStageTransferBit;
                destinationStage = PipelineStageFlags.PipelineStageFragmentShaderBit;
            }
            else if (oldLayout == ImageLayout.Undefined &&
                newLayout == ImageLayout.DepthStencilAttachmentOptimal)
            {
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.AccessDepthStencilAttachmentReadBit |
                    AccessFlags.AccessDepthStencilAttachmentWriteBit;

                sourceStage = PipelineStageFlags.PipelineStageTopOfPipeBit;
                destinationStage = PipelineStageFlags.PipelineStageEarlyFragmentTestsBit;
            }
            else
            {
                throw new Exception("unsupported layout transition!");
            }

            vk.CmdPipelineBarrier(
                commandBuffer,
                sourceStage, destinationStage,
                0,
                0, null,
                0, null,
                1, in barrier
            );

            EndSingleTimeCommands(commandBuffer);
        }

        void CopyBufferToImage(
            Silk.NET.Vulkan.Buffer buffer,
            Image image,
            uint width,
            uint height
            )
        {
            CommandBuffer commandBuffer = BeginSingleTimeCommands();

            BufferImageCopy region = new()
            {
                BufferOffset = 0,
                BufferRowLength = 0,
                BufferImageHeight = 0,
                ImageOffset = new Offset3D(),
                ImageExtent = new Extent3D(width, height, 1)
            };
            region.ImageSubresource.AspectMask = ImageAspectFlags.ImageAspectColorBit;
            region.ImageSubresource.MipLevel = 0;
            region.ImageSubresource.BaseArrayLayer = 0;
            region.ImageSubresource.LayerCount = 1;

            vk.CmdCopyBufferToImage(
                commandBuffer,
                buffer,
                image,
                ImageLayout.TransferDstOptimal,
                1,
                in region
            );

            EndSingleTimeCommands(commandBuffer);
        }

        void CreateTextureImageView()
        {
            textureImageView = CreateImageView(textureImage, Format.R8G8B8A8Srgb,
                ImageAspectFlags.ImageAspectColorBit);
        }

        ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags)
        {
            ImageViewCreateInfo viewInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = image,
                ViewType = ImageViewType.ImageViewType2D,
                Format = format
            };
            viewInfo.SubresourceRange.AspectMask = aspectFlags;
            viewInfo.SubresourceRange.BaseMipLevel = 0;
            viewInfo.SubresourceRange.LevelCount = 1;
            viewInfo.SubresourceRange.BaseArrayLayer = 0;
            viewInfo.SubresourceRange.LayerCount = 1;

            if (vk.CreateImageView(device, in viewInfo, null, out ImageView imageView) != Result.Success)
            {
                throw new Exception("failed to create texture image view!");
            }

            return imageView;
        }

        void CreateTextureSampler()
        {
           
            SamplerCreateInfo samplerInfo = new()
            {
                SType = StructureType.SamplerCreateInfo,
                MagFilter = Filter.Linear,
                MinFilter = Filter.Linear,
                AddressModeU = SamplerAddressMode.Repeat,
                AddressModeV = SamplerAddressMode.Repeat,
                AddressModeW = SamplerAddressMode.Repeat,
                AnisotropyEnable = true,
                MaxAnisotropy = physicalDevice.Properties.Limits.MaxSamplerAnisotropy,
                BorderColor = BorderColor.IntOpaqueBlack,
                UnnormalizedCoordinates = false,
                CompareEnable = false,
                CompareOp = CompareOp.Always,
                MipmapMode = SamplerMipmapMode.Linear,
                MipLodBias = 0.0f,
                MinLod = 0.0f,
                MaxLod = 0.0f
            };

            if (vk.CreateSampler(device, in samplerInfo, null, out textureSampler) != Result.Success)
            {
                throw new Exception("failed to create texture sampler!");
            }
        }

        void CreateVertexBuffer()
        {
            uint bufferSize = (uint)(Marshal.SizeOf(vertices[0]) * vertices.Length);

            CreateBuffer(bufferSize, BufferUsageFlags.BufferUsageTransferSrcBit,
                MemoryPropertyFlags.MemoryPropertyHostVisibleBit |
                    MemoryPropertyFlags.MemoryPropertyHostCoherentBit,
                out Silk.NET.Vulkan.Buffer stagingBuffer,
                out DeviceMemory stagingBufferMemory);

            void* videoMemory;
            vk.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &videoMemory);

            fixed (void* srcData = &vertices[0])
            {
                System.Buffer.MemoryCopy(srcData, videoMemory, bufferSize, bufferSize);
            }

            vk.UnmapMemory(device, stagingBufferMemory);

            CreateBuffer(bufferSize,
                BufferUsageFlags.BufferUsageTransferDstBit |
                    BufferUsageFlags.BufferUsageVertexBufferBit,
                MemoryPropertyFlags.MemoryPropertyHostVisibleBit |
                    MemoryPropertyFlags.MemoryPropertyDeviceLocalBit,
                out vertexBuffer,
                out vertexBufferMemory);

            CopyBuffer(stagingBuffer, vertexBuffer, bufferSize);

            vk.DestroyBuffer(device, stagingBuffer, null);
            vk.FreeMemory(device, stagingBufferMemory, null);

        }

        void CreateBuffer(
            ulong size,
            BufferUsageFlags usage,
            MemoryPropertyFlags properties,
            out Silk.NET.Vulkan.Buffer buffer,
            out DeviceMemory bufferMemory)
        {
            BufferCreateInfo bufferInfo = new()
            {
                SType = StructureType.BufferCreateInfo,
                Size = size,
                Usage = usage,
                SharingMode = SharingMode.Exclusive
            };

            if (vk.CreateBuffer(device, in bufferInfo, null, out buffer) != Result.Success)
            {
                throw new Exception("failed to create buffer!");
            }

            MemoryRequirements memRequirements;
            vk.GetBufferMemoryRequirements(device, buffer, &memRequirements);

            MemoryAllocateInfo allocInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties)
            };

            if (vk.AllocateMemory(device, in allocInfo, null, out bufferMemory) != Result.Success)
            {
                throw new Exception("failed to allocate buffer memory!");
            }

            vk.BindBufferMemory(device, buffer, bufferMemory, 0);
        }

        void CopyBuffer(
            Silk.NET.Vulkan.Buffer srcBuffer,
            Silk.NET.Vulkan.Buffer dstBuffer,
            uint size)
        {
            CommandBuffer commandBuffer = BeginSingleTimeCommands();

            BufferCopy copyRegion = new()
            {
                SrcOffset = 0, // Optional
                DstOffset = 0, // Optional
                Size = size
            };

            vk.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, in copyRegion);

            EndSingleTimeCommands(commandBuffer);
        }

        CommandBuffer BeginSingleTimeCommands()
        {
            CommandBufferAllocateInfo allocInfo = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                Level = CommandBufferLevel.Primary,
                CommandPool = commandPool,
                CommandBufferCount = 1
            };

            vk.AllocateCommandBuffers(device, in allocInfo, out CommandBuffer commandBuffer);

            CommandBufferBeginInfo beginInfo = new();
            beginInfo.SType = StructureType.CommandBufferBeginInfo;
            beginInfo.Flags = CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit;

            vk.BeginCommandBuffer(commandBuffer, in beginInfo);

            return commandBuffer;
        }

        void EndSingleTimeCommands(CommandBuffer commandBuffer)
        {
            vk.EndCommandBuffer(commandBuffer);

            SubmitInfo submitInfo = new();
            submitInfo.SType = StructureType.SubmitInfo;
            submitInfo.CommandBufferCount = 1;
            submitInfo.PCommandBuffers = &commandBuffer;

            vk.QueueSubmit(graphicsQueue, 1, in submitInfo, new Fence(null));
            vk.QueueWaitIdle(graphicsQueue);

            vk.FreeCommandBuffers(device, commandPool, 1, in commandBuffer);
        }

        uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
        {            
            for (int i = 0; i < physicalDevice.MemoryProperties.MemoryTypeCount; i++)
            {
                if ((typeFilter & (i << 1)) != 0 &&
                    (physicalDevice.MemoryProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
                {
                    return (uint)i;
                }
            }

            throw new Exception("failed to find suitable memory type!");
        }

        void CreateIndexBuffer()
        {
            uint bufferSize = (uint)(Marshal.SizeOf(indices[0]) * indices.Length);

            CreateBuffer(bufferSize, BufferUsageFlags.BufferUsageTransferSrcBit,
                MemoryPropertyFlags.MemoryPropertyHostVisibleBit |
                    MemoryPropertyFlags.MemoryPropertyHostCoherentBit,
                out Silk.NET.Vulkan.Buffer stagingBuffer,
                out DeviceMemory stagingBufferMemory);

            void* videoMemory;
            vk.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &videoMemory);

            fixed (void* srcData = &indices[0])
            {
                System.Buffer.MemoryCopy(srcData, videoMemory, bufferSize, bufferSize);
            }

            vk.UnmapMemory(device, stagingBufferMemory);

            CreateBuffer(bufferSize,
                BufferUsageFlags.BufferUsageTransferDstBit |
                    BufferUsageFlags.BufferUsageIndexBufferBit,
                MemoryPropertyFlags.MemoryPropertyHostVisibleBit |
                    MemoryPropertyFlags.MemoryPropertyDeviceLocalBit,
                out indexBuffer,
                out indexBufferMemory);

            CopyBuffer(stagingBuffer, indexBuffer, bufferSize);

            vk.DestroyBuffer(device, stagingBuffer, null);
            vk.FreeMemory(device, stagingBufferMemory, null);
        }

        void CreateUniformBuffers()
        {
            uint bufferSize = (uint)sizeof(UniformBufferObject);

            uniformBuffers = new Silk.NET.Vulkan.Buffer[MAX_FRAMES_IN_FLIGHT];
            uniformBuffersMemory = new DeviceMemory[MAX_FRAMES_IN_FLIGHT];

            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                CreateBuffer(bufferSize, BufferUsageFlags.BufferUsageUniformBufferBit,
                    MemoryPropertyFlags.MemoryPropertyHostVisibleBit |
                    MemoryPropertyFlags.MemoryPropertyHostCoherentBit,
                    out uniformBuffers[i], out uniformBuffersMemory[i]);
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
                    Buffer = uniformBuffers[i],
                    Offset = 0,
                    Range = (ulong)sizeof(UniformBufferObject)
                };

                DescriptorImageInfo imageInfo = new();
                imageInfo.ImageLayout = ImageLayout.ShaderReadOnlyOptimal;
                imageInfo.ImageView = textureImageView;
                imageInfo.Sampler = textureSampler;

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
            commandBuffers = new CommandBuffer[MAX_FRAMES_IN_FLIGHT];

            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                CommandBufferAllocateInfo allocInfo = new()
                {
                    SType = StructureType.CommandBufferAllocateInfo,
                    CommandPool = commandPool,
                    Level = CommandBufferLevel.Primary,
                    CommandBufferCount = 1
                };

                if (vk.AllocateCommandBuffers(device, in allocInfo, out commandBuffers[i])
                    != Result.Success)
                {
                    throw new Exception("failed to allocate command buffers!");
                }
            }
        }

        void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex)
        {
            CommandBufferBeginInfo beginInfo = new()
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = 0, // Optional
                PInheritanceInfo = null // Optional
            };

            if (vk.BeginCommandBuffer(commandBuffer, in beginInfo) != Result.Success)
            {
                throw new Exception("failed to begin recording command buffer!");
            }

            ClearValue* clearValues = stackalloc[]
                {
                    new ClearValue(new ClearColorValue(0, 0, 0, 1)),
                    new ClearValue(null, new ClearDepthStencilValue(1.0f, 0))
                };

            RenderPassBeginInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = renderPass,
                Framebuffer = swapChainFramebuffers[imageIndex],
                ClearValueCount = 2,
                PClearValues = clearValues
            };

            renderPassInfo.RenderArea.Offset = new Offset2D(0, 0);
            renderPassInfo.RenderArea.Extent = swapChainExtent;

            vk.CmdBeginRenderPass(commandBuffer, in renderPassInfo, SubpassContents.Inline);

            vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, graphicsPipeline);

            ulong offsets = 0;
            vk.CmdBindVertexBuffers(commandBuffer, 0, 1, in vertexBuffer, in offsets);

            vk.CmdBindIndexBuffer(commandBuffer, indexBuffer, 0, IndexType.Uint16);

            vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, pipelineLayout,
                0, 1, in descriptorSets[currentFrame], 0, null);

            vk.CmdDrawIndexed(commandBuffer, (uint)indices.Length, 1, 0, 0, 0);

            vk.CmdEndRenderPass(commandBuffer);

            if (vk.EndCommandBuffer(commandBuffer) != Result.Success)
            {
                throw new Exception("failed to record command buffer!");
            }
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
                if ((vk.CreateSemaphore(device, in semaphoreInfo, null, out imageAvailableSemaphores[i]) != Result.Success) ||
                    (vk.CreateSemaphore(device, in semaphoreInfo, null, out renderFinishedSemaphores[i]) != Result.Success) ||
                    (vk.CreateFence(device, in fenceInfo, null, out inFlightFences[i]) != Result.Success))
                {
                    throw new Exception("failed to create semaphores!");
                }
            }
        }

        void DrawFrame()
        {

            vk.WaitForFences(device, 1, in inFlightFences[currentFrame], true, ulong.MaxValue);

            uint imageIndex = 0;

            var result = vkSwapChain.AcquireNextImage(device, swapChain, ulong.MaxValue,
                imageAvailableSemaphores[currentFrame], new Fence(null), ref imageIndex);

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

            vk.ResetFences(device, 1, in inFlightFences[currentFrame]);

            vk.ResetCommandBuffer(commandBuffers[currentFrame], 0);
            RecordCommandBuffer(commandBuffers[currentFrame], imageIndex);

            UpdateUniformBuffer(imageIndex);

            Semaphore* waitSemaphores = stackalloc[] { imageAvailableSemaphores[currentFrame] };
            PipelineStageFlags* waitStages = stackalloc[] { PipelineStageFlags.PipelineStageColorAttachmentOutputBit };
            Semaphore signalSemaphore = renderFinishedSemaphores[currentFrame];

            SubmitInfo submitInfo = new()
            {
                SType = StructureType.SubmitInfo,
                WaitSemaphoreCount = 1
            };

            fixed (CommandBuffer* commandBufferPtr = &commandBuffers[currentFrame])
            {
                submitInfo.WaitSemaphoreCount = 1;
                submitInfo.PWaitSemaphores = waitSemaphores;
                submitInfo.PWaitDstStageMask = waitStages;

                submitInfo.CommandBufferCount = 1;
                submitInfo.PCommandBuffers = commandBufferPtr;

                submitInfo.SignalSemaphoreCount = 1;
                submitInfo.PSignalSemaphores = &signalSemaphore;

                if (vk.QueueSubmit(graphicsQueue, 1, in submitInfo, inFlightFences[currentFrame]) != Result.Success)
                {
                    throw new Exception("failed to submit draw command buffer!");
                }

                currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
            }

            fixed (SwapchainKHR* swapChainPtr = &swapChain)
            {
                PresentInfoKHR presentInfo = new();
                presentInfo.SType = StructureType.PresentInfoKhr;
                presentInfo.WaitSemaphoreCount = 1;
                presentInfo.PWaitSemaphores = &signalSemaphore;
                presentInfo.SwapchainCount = 1;
                presentInfo.PSwapchains = swapChainPtr;
                presentInfo.PImageIndices = &imageIndex;
                presentInfo.PResults = null;

                vkSwapChain.QueuePresent(presentQueue, in presentInfo);
            }

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

            void* videoMemory;
            ulong sizeBytes = (ulong)Marshal.SizeOf(ubo);
            vk.MapMemory(device, uniformBuffersMemory[currentFrame], 0, sizeBytes, 0, &videoMemory);

            System.Buffer.MemoryCopy(&ubo, videoMemory, sizeBytes, sizeBytes);

            vk.UnmapMemory(device, uniformBuffersMemory[currentFrame]);
        }

        void RecreateSwapChain()
        {
            while (window.Size.X == 0 || window.Size.Y == 0)
            {
                window.DoEvents();
            }

            vk.DeviceWaitIdle(device);

            CleanupSwapChain();

            CreateSwapChain();
            CreateImageViews();
            CreateRenderPass();
            CreateGraphicsPipeline();
            CreateDepthResources();
            CreateFramebuffers();
        }

        void CleanupSwapChain()
        {
            vk.DestroyImageView(device, depthImageView, null);
            vk.DestroyImage(device, depthImage, null);
            vk.FreeMemory(device, depthImageMemory, null);

            foreach (var framebuffer in swapChainFramebuffers)
            {
                vk.DestroyFramebuffer(device, framebuffer, null);
            }

            vk.DestroyPipeline(device, graphicsPipeline, null);
            vk.DestroyPipelineLayout(device, pipelineLayout, null);
            vk.DestroyRenderPass(device, renderPass, null);

            foreach (var imageView in swapChainImageViews)
            {
                vk.DestroyImageView(device, imageView, null);
            }

            vkSwapChain.DestroySwapchain(device, swapChain, null);
        }

        void Cleanup()
        {
            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                vk.DestroySemaphore(device, imageAvailableSemaphores[i], null);
                vk.DestroySemaphore(device, renderFinishedSemaphores[i], null);
                vk.DestroyFence(device, inFlightFences[i], null);
            }

            vk.DestroyCommandPool(device, commandPool, null);

            CleanupSwapChain();

            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                vk.DestroyBuffer(device, uniformBuffers[i], null);
                vk.FreeMemory(device, uniformBuffersMemory[i], null);
            }

            vk.DestroySampler(device, textureSampler, null);
            vk.DestroyImageView(device, textureImageView, null);
            vk.DestroyImage(device, textureImage, null);
            vk.FreeMemory(device, textureImageMemory, null);

            vk.DestroyDescriptorPool(device, descriptorPool, null);
            vk.DestroyDescriptorSetLayout(device, descriptorSetLayout, null);

            vk.DestroyBuffer(device, indexBuffer, null);
            vk.FreeMemory(device, indexBufferMemory, null);

            vk.DestroyBuffer(device, vertexBuffer, null);
            vk.FreeMemory(device, vertexBufferMemory, null);

            vkDevice.Dispose();

            surfaceData.Dispose();
            vkInstance.Dispose();
   
            window.Close();
            window.Dispose();

        }

    }

}
