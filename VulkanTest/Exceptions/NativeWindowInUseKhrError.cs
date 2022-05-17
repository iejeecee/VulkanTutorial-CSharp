using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class NativeWindowInUseKhrError : Exception
    {
        public NativeWindowInUseKhrError()
        {
        }

        public NativeWindowInUseKhrError(string message) : base(message)
        {
        }

        public NativeWindowInUseKhrError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NativeWindowInUseKhrError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}