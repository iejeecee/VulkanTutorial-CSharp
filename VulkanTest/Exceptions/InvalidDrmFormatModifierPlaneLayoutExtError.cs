using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class InvalidDrmFormatModifierPlaneLayoutExtError : Exception
    {
        public InvalidDrmFormatModifierPlaneLayoutExtError()
        {
        }

        public InvalidDrmFormatModifierPlaneLayoutExtError(string message) : base(message)
        {
        }

        public InvalidDrmFormatModifierPlaneLayoutExtError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidDrmFormatModifierPlaneLayoutExtError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}