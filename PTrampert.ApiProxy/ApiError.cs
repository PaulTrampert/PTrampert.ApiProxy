using System;
using System.Collections.Generic;
using System.Text;

namespace PTrampert.ApiProxy
{
    public class ApiError
    {
        public string Message { get; set; }

        public ApiError(string message)
        {
            Message = message;
        }
    }
}
