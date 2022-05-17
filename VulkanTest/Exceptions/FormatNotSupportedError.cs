using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class FormatNotSupportedError : Exception
    {
        public FormatNotSupportedError()
        {
        }

        public FormatNotSupportedError(string message) : base(message)
        {
        }

        public FormatNotSupportedError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FormatNotSupportedError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}