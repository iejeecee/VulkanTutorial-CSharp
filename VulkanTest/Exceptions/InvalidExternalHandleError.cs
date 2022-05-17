using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class InvalidExternalHandleError : Exception
    {
        public InvalidExternalHandleError()
        {
        }

        public InvalidExternalHandleError(string message) : base(message)
        {
        }

        public InvalidExternalHandleError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidExternalHandleError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}