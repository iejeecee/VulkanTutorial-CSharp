using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class ValidationFailedExtError : Exception
    {
        public ValidationFailedExtError()
        {
        }

        public ValidationFailedExtError(string message) : base(message)
        {
        }

        public ValidationFailedExtError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ValidationFailedExtError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}