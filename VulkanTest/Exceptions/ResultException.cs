using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VulkanTest.Exceptions
{
    class ResultException : Exception
    {
        public ResultException(string error) :
            base(error)
        {


        }
    }
}
