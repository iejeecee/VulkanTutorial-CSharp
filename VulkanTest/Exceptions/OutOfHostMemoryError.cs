using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class OutOfHostMemoryError : Exception
    {
        public OutOfHostMemoryError()
        {
        }

        public OutOfHostMemoryError(string message) : base(message)
        {
        }

        public OutOfHostMemoryError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected OutOfHostMemoryError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
