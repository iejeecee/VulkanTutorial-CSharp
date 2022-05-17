using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class ExtensionNotPresentError : Exception
    {
        public ExtensionNotPresentError()
        {
        }

        public ExtensionNotPresentError(string message) : base(message)
        {
        }

        public ExtensionNotPresentError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ExtensionNotPresentError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}