using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class InvalidShaderNVError : Exception
    {
        public InvalidShaderNVError()
        {
        }

        public InvalidShaderNVError(string message) : base(message)
        {
        }

        public InvalidShaderNVError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidShaderNVError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}