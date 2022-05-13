using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VulkanTest.Utils
{
    struct QueueFamilyIndices
    {
        public uint? GraphicsFamily { get; set; }
        public uint? PresentFamily { get; set; }

        public bool IsComplete
        {
            get
            {
                return GraphicsFamily.HasValue && PresentFamily.HasValue;
            }
        }
    }
}
