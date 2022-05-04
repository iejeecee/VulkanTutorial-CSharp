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

namespace VulkanTest.VkOO
{
    unsafe class VkDevice : IDisposable
    {
        Device device;
        Vk vk;
        private bool disposedValue;

        public VkDevice(
            VkPhysicalDevice physicalDevice,
            QueueFamilyIndices indices,
            PhysicalDeviceFeatures deviceFeatures,
            string[] validationLayers = null,
            string[] deviceExtensions = null            
            )
        {
            float queuePriority = 1.0f;
            vk = physicalDevice.Vk;

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

            /*PhysicalDeviceFeatures deviceFeatures = new()
            {
                SamplerAnisotropy = true
            };*/

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
          
            if (vk.CreateDevice(physicalDevice.Device, in createInfo, null, out device) != Result.Success)
            {
                throw new Exception("failed to create logical device!");
            }
        }

        public Queue GetDeviceQueue(uint queueFamilyIndex, uint queueIndex = 0)
        {
            vk.GetDeviceQueue(device, queueFamilyIndex, queueIndex, out Queue queue);

            return queue;
        }

        public Device Device { get => device; protected set => device = value; }
        public Vk Vk { get => vk; protected set => vk = value; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                vk.DestroyDevice(device, null);

                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~VkDevice()
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
