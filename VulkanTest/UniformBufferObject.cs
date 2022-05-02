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
    struct UniformBufferObject
    {
        public Matrix4X4<float> model;
        public Matrix4X4<float> view;
        public Matrix4X4<float> proj;

    }
}
