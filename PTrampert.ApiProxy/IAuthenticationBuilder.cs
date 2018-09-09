namespace PTrampert.ApiProxy
{
    internal interface IAuthenticationBuilder
    {
        IAuthentication BuildAuthentication(ApiConfig config);
    }
}