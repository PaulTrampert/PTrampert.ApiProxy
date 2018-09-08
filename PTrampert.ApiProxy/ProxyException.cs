using System;

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
