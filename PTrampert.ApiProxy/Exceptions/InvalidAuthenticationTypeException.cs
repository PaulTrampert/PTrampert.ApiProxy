using System;

namespace PTrampert.ApiProxy.Exceptions
{
    /// <summary>
    /// Exeption thrown when an <see cref="IAuthentication"/> cannot be created because the requested
    /// type does not implement <see cref="IAuthentication"/>.
    /// </summary>
    public class InvalidAuthenticationTypeException : Exception
    {
        /// <summary>
        /// Constructor for <see cref="InvalidAuthenticationTypeException"/>
        /// </summary>
        /// <param name="type">The offending <see cref="Type"/></param>
        public InvalidAuthenticationTypeException(Type type) : base(
            $"{type.FullName} does not implement {typeof(IAuthentication).FullName}")
        { }
    }
}
