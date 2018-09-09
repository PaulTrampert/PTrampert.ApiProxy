using System;
using System.Collections.Generic;
using System.Text;
using PTrampert.ApiProxy.Authentication;

namespace PTrampert.ApiProxy.Exceptions
{
    public class InvalidTokenModeException : Exception
    {
        public InvalidTokenModeException(string mode) : base($"Invalid token mode specified: {mode}") { }
    }
}
