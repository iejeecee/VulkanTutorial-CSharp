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

namespace VulkanTest
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct Vertex
    {
        Vector3D<float> pos;
        Vector3D<float> color;
        Vector2D<float> texCoord;

        public Vertex(Vector3D<float> pos, Vector3D<float> color, Vector2D<float> texCoord)
        {
            this.pos = pos;
            this.color = color;
            this.texCoord = texCoord;
        }
        
        public static uint GetStride()
        {
            return (uint)sizeof(Vertex);
        }

        public static (Format format, uint offset)[] GetAttributeFormatsAndOffsets()
        {
            (Format, uint)[] results = new (Format, uint)[]
                {
                    new (Format.R32G32B32Sfloat, (uint)Marshal.OffsetOf(typeof(Vertex), nameof(pos))),
                    new (Format.R32G32B32Sfloat, (uint)Marshal.OffsetOf(typeof(Vertex), nameof(color))),
                    new (Format.R32G32Sfloat, (uint)Marshal.OffsetOf(typeof(Vertex), nameof(texCoord)))
                };

            return results;
        }
    }
}
