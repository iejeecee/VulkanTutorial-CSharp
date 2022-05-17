using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class SurfaceLostKhrError : Exception
    {
        public SurfaceLostKhrError()
        {
        }

        public SurfaceLostKhrError(string message) : base(message)
        {
        }

        public SurfaceLostKhrError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SurfaceLostKhrError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}