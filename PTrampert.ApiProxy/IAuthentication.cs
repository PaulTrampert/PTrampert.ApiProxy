using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PTrampert.ApiProxy
{
    public interface IAuthentication
    {
        Task<AuthenticationHeaderValue> GetAuthenticationHeader();
    }
}
