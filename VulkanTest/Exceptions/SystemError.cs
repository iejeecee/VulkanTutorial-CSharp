using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    internal class SystemError : Exception
    {
        public SystemError()
        {
        }

        public SystemError(string message) : base(message)
        {
        }

        public SystemError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SystemError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}