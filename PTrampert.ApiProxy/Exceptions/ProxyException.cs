using System;

namespace PTrampert.ApiProxy.Exceptions
{
    /// <summary>
    /// Exception thrown when there is an error proxying a request.
    /// </summary>
    public class ProxyException : Exception
    {
        /// <summary>
        /// The HTTP Status code to return to the client.
        /// </summary>
        public int Status { get; }

        /// <summary>
        /// Constructor for <see cref="ProxyException"/>.
        /// </summary>
        /// <param name="message">The message explaining the error.</param>
        /// <param name="status">The HTTP status code to return to the client.</param>
        public ProxyException(string message, int status) : base(message)
        {
            Status = status;
        }
    }
}
