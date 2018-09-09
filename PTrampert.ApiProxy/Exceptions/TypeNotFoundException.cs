﻿using System;

namespace PTrampert.ApiProxy.Exceptions
{
    public class TypeNotFoundException : Exception
    {
        public TypeNotFoundException(string name) : base($"No type matching {name} was found.")
        {
        }
    }
}
