using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PTrampert.ApiProxy
{
    public interface IAuthentication
    {
        Task<AuthenticationHeaderValue> GetAuthenticationHeader();
    }
}
