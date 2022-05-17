using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class FeatureNotPresentError : Exception
    {
        public FeatureNotPresentError()
        {
        }

        public FeatureNotPresentError(string message) : base(message)
        {
        }

        public FeatureNotPresentError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FeatureNotPresentError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}