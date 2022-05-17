using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class MemoryMapFailedError : Exception
    {
        public MemoryMapFailedError()
        {
        }

        public MemoryMapFailedError(string message) : base(message)
        {
        }

        public MemoryMapFailedError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MemoryMapFailedError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}