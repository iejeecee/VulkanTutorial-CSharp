using System;
using System.Runtime.Serialization;

namespace VulkanTest.Exceptions
{
    [Serializable]
    public class FragmentedPoolError : Exception
    {
        public FragmentedPoolError()
        {
        }

        public FragmentedPoolError(string message) : base(message)
        {
        }

        public FragmentedPoolError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FragmentedPoolError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}