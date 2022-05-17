using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class IncompatibleDisplayKhrError : Exception
    {
        public IncompatibleDisplayKhrError()
        {
        }

        public IncompatibleDisplayKhrError(string message) : base(message)
        {
        }

        public IncompatibleDisplayKhrError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected IncompatibleDisplayKhrError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}