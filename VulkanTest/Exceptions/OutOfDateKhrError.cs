using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class OutOfDateKhrError : Exception
    {
        public OutOfDateKhrError()
        {
        }

        public OutOfDateKhrError(string message) : base(message)
        {
        }

        public OutOfDateKhrError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected OutOfDateKhrError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}