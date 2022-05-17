using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class OutOfPoolMemoryError : Exception
    {
        public OutOfPoolMemoryError()
        {
        }

        public OutOfPoolMemoryError(string message) : base(message)
        {
        }

        public OutOfPoolMemoryError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected OutOfPoolMemoryError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}