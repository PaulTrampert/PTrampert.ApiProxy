using System;

namespace PTrampert.ApiProxy.Exceptions
{
    /// <summary>
    /// Thrown by <see cref="IAuthenticationFactory" /> when the requested <see cref="IAuthentication"/> type is not found.
    /// </summary>
    public class TypeNotFoundException : Exception
    {
        /// <summary>
        /// Constructor for <see cref="TypeNotFoundException"/>
        /// </summary>
        /// <param name="name">The requested type's name.</param>
        public TypeNotFoundException(string name) : base($"No type matching {name} was found.")
        {
        }
    }
}
