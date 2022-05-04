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
    unsafe class VkInstance : IDisposable
    {    
        Vk vk;
        Instance instance;
     
        DebugUtilsMessengerEXT debugMessenger;
        ExtDebugUtils debugUtils;
        DebugUtilsMessengerCallbackFunctionEXT callbackFunc;

        string[] validationLayers;
        private bool disposedValue;

        public VkInstance(          
            string appName,
            string engineName,
            Version32 apiVersion,
            string[] extensions = null,
            string[] layers = null,
            DebugUtilsMessengerCallbackFunctionEXT callbackFunc = null)
        {          
            if (extensions == null) extensions = Array.Empty<string>();
            this.callbackFunc = callbackFunc ?? MessageCallback;

            Vk = Vk.GetApi();

            if (layers != null)
            {
                validationLayers = CheckAvailableValidationLayers(layers);
                if (validationLayers is null)
                {
                    throw new NotSupportedException("Validation layers requested, but not available!");
                }
            }

            ApplicationInfo appInfo = new()
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName = (byte*)Marshal.StringToHGlobalAnsi(appName),
                ApplicationVersion = new Version32(1, 0, 0),
                PEngineName = (byte*)Marshal.StringToHGlobalAnsi(engineName),
                EngineVersion = new Version32(1, 0, 0),
                ApiVersion = apiVersion
            };

            InstanceCreateInfo createInfo = new()
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo
            };

            var newExtensions = stackalloc byte*[extensions.Length];

            for (var i = 0; i < extensions.Length; i++)
            {
                newExtensions[i] = (byte*)SilkMarshal.StringToPtr(extensions[i]);
            }

            createInfo.EnabledExtensionCount = (uint)extensions.Length;
            createInfo.PpEnabledExtensionNames = newExtensions;

            if (validationLayers != null)
            {
                createInfo.EnabledLayerCount = (uint)validationLayers.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);

                DebugUtilsMessengerCreateInfoEXT messengerInfo = GetDebugMessengerCreateInfo();

                createInfo.PNext = &messengerInfo;
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
                createInfo.PNext = null;
            }

            Result result = Vk.CreateInstance(in createInfo, null, out instance);
            if (result != Result.Success)
            {
                throw new Exception("Failed to create instance!");
            }

            Vk.CurrentInstance = Instance;
        
            for (var i = 0; i < extensions.Length; i++)
            {
                SilkMarshal.Free((nint)newExtensions[i]);
            }

            Marshal.FreeHGlobal((nint)appInfo.PApplicationName);
            Marshal.FreeHGlobal((nint)appInfo.PEngineName);
          
            if (validationLayers != null)
            {
                CreateDebugMessenger();
            }
        }
        
        void CreateDebugMessenger()
        {
            if (!Vk.TryGetInstanceExtension(Instance, out debugUtils)) return;

            DebugUtilsMessengerCreateInfoEXT createInfo = GetDebugMessengerCreateInfo();

            if (debugUtils.CreateDebugUtilsMessenger(Instance, in createInfo, null,
                out debugMessenger) != Result.Success)
            {
                throw new Exception("Failed to create debug messenger.");
            }
        }

        DebugUtilsMessengerCreateInfoEXT GetDebugMessengerCreateInfo()
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
                PfnUserCallback = new PfnDebugUtilsMessengerCallbackEXT(callbackFunc),
                PUserData = null
            };

            return createInfo;
        }

        string[] CheckAvailableValidationLayers(string[] layers)
        {
            uint nrLayers = 0;
            Vk.EnumerateInstanceLayerProperties(&nrLayers, null);

            LayerProperties[] availableLayers = new LayerProperties[nrLayers];

            Vk.EnumerateInstanceLayerProperties(&nrLayers, availableLayers);

            var availableLayerNames = availableLayers.Select(availableLayer => Marshal.PtrToStringAnsi((nint)availableLayer.LayerName)).ToArray();

            if (layers.All(validationLayerName => availableLayerNames.Contains(validationLayerName)))
            {
                return layers;
            }

            return null;
        }

        static uint MessageCallback(DebugUtilsMessageSeverityFlagsEXT severity, DebugUtilsMessageTypeFlagsEXT messageType, DebugUtilsMessengerCallbackDataEXT* callbackData, void* userData)
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

        public VkPhysicalDevice[] EnumerateDevices()
        {
            uint deviceCount = 0;
            vk.EnumeratePhysicalDevices(instance, &deviceCount, null);

            if (deviceCount == 0)
            {
                throw new Exception("failed to find GPUs with Vulkan support!");
            }

            PhysicalDevice[] devices = new PhysicalDevice[deviceCount];

            vk.EnumeratePhysicalDevices(instance, &deviceCount, devices);

            VkPhysicalDevice[] vkPhysicalDevices = new VkPhysicalDevice[deviceCount];

            for (int i = 0; i < deviceCount; i++)
            {
                vkPhysicalDevices[i] = new VkPhysicalDevice(devices[i], vk);
            }

            return vkPhysicalDevices;
        }

        public Vk Vk { get => vk; protected set => vk = value; }
        public Instance Instance { get => instance; protected set => instance = value; }       
        
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }
              
                if (validationLayers != null)
                {
                    debugUtils.DestroyDebugUtilsMessenger(Instance, debugMessenger, null);
                }

                Vk.DestroyInstance(Instance, null);

                disposedValue = true;
            }
        }

         // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~VkInstance()
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
