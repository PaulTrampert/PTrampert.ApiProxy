namespace PTrampert.ApiProxy
{
    public interface IAuthenticationBuilder
    {
        IAuthentication BuildAuthentication(ApiConfig config);
    }
}