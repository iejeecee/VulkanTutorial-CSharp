using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class NotPermittedKhrError : Exception
    {
        public NotPermittedKhrError()
        {
        }

        public NotPermittedKhrError(string message) : base(message)
        {
        }

        public NotPermittedKhrError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NotPermittedKhrError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}