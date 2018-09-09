using System;

namespace PTrampert.ApiProxy.Exceptions
{
    public class InvalidAuthenticationTypeException : Exception
    {
        public InvalidAuthenticationTypeException(Type type) : base(
            $"{type.FullName} does not implement {typeof(IAuthentication).FullName}")
        { }
    }
}
