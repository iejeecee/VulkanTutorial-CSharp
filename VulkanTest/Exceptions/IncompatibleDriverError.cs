using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class IncompatibleDriverError : Exception
    {
        public IncompatibleDriverError()
        {
        }

        public IncompatibleDriverError(string message) : base(message)
        {
        }

        public IncompatibleDriverError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected IncompatibleDriverError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}