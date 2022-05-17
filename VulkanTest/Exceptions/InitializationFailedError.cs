using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class InitializationFailedError : Exception
    {
        public InitializationFailedError()
        {
        }

        public InitializationFailedError(string message) : base(message)
        {
        }

        public InitializationFailedError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InitializationFailedError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}