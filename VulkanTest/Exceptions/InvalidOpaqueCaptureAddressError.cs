using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class InvalidOpaqueCaptureAddressError : Exception
    {
        public InvalidOpaqueCaptureAddressError()
        {
        }

        public InvalidOpaqueCaptureAddressError(string message) : base(message)
        {
        }

        public InvalidOpaqueCaptureAddressError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidOpaqueCaptureAddressError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}