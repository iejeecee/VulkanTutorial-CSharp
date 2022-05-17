using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class LayerNotPresentError : Exception
    {
        public LayerNotPresentError()
        {
        }

        public LayerNotPresentError(string message) : base(message)
        {
        }

        public LayerNotPresentError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected LayerNotPresentError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}