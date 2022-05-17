using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class DeviceLostError : Exception
    {
        public DeviceLostError()
        {
        }

        public DeviceLostError(string message) : base(message)
        {
        }

        public DeviceLostError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DeviceLostError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}