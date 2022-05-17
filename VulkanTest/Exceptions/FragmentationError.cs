using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class FragmentationError : Exception
    {
        public FragmentationError()
        {
        }

        public FragmentationError(string message) : base(message)
        {
        }

        public FragmentationError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FragmentationError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}