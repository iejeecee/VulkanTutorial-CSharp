using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class UnknownError : Exception
    {
        public UnknownError()
        {
        }

        public UnknownError(string message) : base(message)
        {
        }

        public UnknownError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnknownError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}