using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class OutOfDeviceMemoryError : Exception
    {
        public OutOfDeviceMemoryError()
        {
        }

        public OutOfDeviceMemoryError(string message) : base(message)
        {
        }

        public OutOfDeviceMemoryError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected OutOfDeviceMemoryError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}