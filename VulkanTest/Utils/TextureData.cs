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
using VulkanTest.VulkanObject;

namespace VulkanTest.Utils
{
    unsafe class TextureData : IDisposable
    {
        public VkSampler sampler;
        public Format format;
        public Extent2D extent;
        readonly bool needsStaging;
        public BufferData stagingBufferData;
        public ImageData imageData;
        private bool disposedValue;

        public TextureData(VkPhysicalDevice physicalDevice,
                     VkDevice device,
                     Extent2D extent,
                     ImageUsageFlags usageFlags,
                     FormatFeatureFlags formatFeatureFlags,
                     bool anisotropyEnable = false,
                     bool forceStaging = false)
        {

            format = Format.R8G8B8A8Srgb;
            this.extent = extent;

            sampler = new VkSampler(device, new(
                magFilter: Filter.Linear,
                minFilter: Filter.Linear,
                mipmapMode: SamplerMipmapMode.Linear,
                addressModeU: SamplerAddressMode.Repeat,
                addressModeV: SamplerAddressMode.Repeat,
                addressModeW: SamplerAddressMode.Repeat,
                anisotropyEnable: anisotropyEnable,
                maxAnisotropy: 16.0f,
                compareEnable: false,
                compareOp: CompareOp.Never,
                borderColor: BorderColor.FloatOpaqueBlack));


            FormatProperties formatProperties = physicalDevice.GetFormatProperties(format);

            formatFeatureFlags |= FormatFeatureFlags.FormatFeatureSampledImageBit;
            needsStaging = forceStaging || ((formatProperties.LinearTilingFeatures & formatFeatureFlags) != formatFeatureFlags);
            ImageTiling imageTiling;
            ImageLayout initialLayout;
            MemoryPropertyFlags requirements;

            if (needsStaging)
            {
                Debug.Assert(formatProperties.OptimalTilingFeatures.HasFlag(formatFeatureFlags));
                stagingBufferData = new BufferData(physicalDevice, device, extent.Width * extent.Height * 4, BufferUsageFlags.BufferUsageTransferSrcBit);
                imageTiling = ImageTiling.Optimal;
                requirements = MemoryPropertyFlags.MemoryPropertyDeviceLocalBit;
                usageFlags |= ImageUsageFlags.ImageUsageTransferDstBit;
                initialLayout = ImageLayout.Undefined;
            }
            else
            {
                imageTiling = ImageTiling.Linear;
                initialLayout = ImageLayout.Preinitialized;
                requirements = MemoryPropertyFlags.MemoryPropertyHostCoherentBit |
                    MemoryPropertyFlags.MemoryPropertyHostVisibleBit;
            }

            imageData = new ImageData(physicalDevice,
                                   device,
                                   format,
                                   extent,
                                   imageTiling,
                                   usageFlags | ImageUsageFlags.ImageUsageSampledBit,
                                   initialLayout,
                                   requirements,
                                   ImageAspectFlags.ImageAspectColorBit);
            
        }

        public void SetImage(VkCommandBuffer commandBuffer, void* srcData, ulong srcDataSize)
        {
            void* data = needsStaging ? stagingBufferData.deviceMemory.MapMemory(0, stagingBufferData.buffer.GetMemoryRequirements().Size)
                                        : imageData.deviceMemory.MapMemory(0, imageData.image.GetMemoryRequirements().Size);

            System.Buffer.MemoryCopy(srcData, data, srcDataSize, srcDataSize);

            if (needsStaging)
            {
                stagingBufferData.deviceMemory.UnmapMemory();
            }
            else
            {
                imageData.deviceMemory.UnmapMemory();
            }

            if (needsStaging)
            {
                // Since we're going to blit to the texture image, set its layout to eTransferDstOptimal
                SU.SetImageLayout(commandBuffer, imageData.image, imageData.format, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);

                BufferImageCopy copyRegion = new(0,
                                                extent.Width,
                                                extent.Height,
                                                new ImageSubresourceLayers(ImageAspectFlags.ImageAspectColorBit, 0, 0, 1),
                                                new Offset3D(0, 0, 0),
                                                new Extent3D(extent.Width, extent.Height, 1));

                commandBuffer.CopyBufferToImage(stagingBufferData.buffer, imageData.image,
                    ImageLayout.TransferDstOptimal, copyRegion);

                // Set the layout for the texture image from eTransferDstOptimal to eShaderReadOnlyOptimal
                SU.SetImageLayout(commandBuffer, imageData.image, imageData.format,
                    ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);
            }
            else
            {
                // If we can use the linear tiled image as a texture, just do it
                SU.SetImageLayout(commandBuffer, imageData.image, imageData.format,
                    ImageLayout.Preinitialized, ImageLayout.ShaderReadOnlyOptimal);
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                sampler.Dispose();
                stagingBufferData?.Dispose();
                imageData.Dispose();

                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~TextureData()
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
