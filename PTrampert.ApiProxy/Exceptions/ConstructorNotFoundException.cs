using System;
using System.Collections.Generic;
using System.Text;

namespace PTrampert.ApiProxy.Exceptions
{
    public class ConstructorNotFoundException : Exception
    {
        public ConstructorNotFoundException(IDictionary<string, string> arguments) : base($"No constructor found matching arguments with keys: {string.Join(", ", arguments.Keys)}")
        {

        }
    }
}
