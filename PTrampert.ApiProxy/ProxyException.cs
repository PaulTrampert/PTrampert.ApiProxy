using System;
using System.Collections.Generic;
using System.Text;

namespace PTrampert.ApiProxy
{
    public class ProxyException : Exception
    {
        public int Status { get; }
        public ProxyException(string message, int status) : base(message)
        {
            Status = status;
        }
    }
}
