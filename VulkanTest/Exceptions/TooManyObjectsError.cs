using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class TooManyObjectsError : Exception
    {
        public TooManyObjectsError()
        {
        }

        public TooManyObjectsError(string message) : base(message)
        {
        }

        public TooManyObjectsError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TooManyObjectsError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}