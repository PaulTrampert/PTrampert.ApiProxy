namespace PTrampert.ApiProxy
{
    /// <summary>
    /// Factory interface for providing an <see cref="IAuthentication" /> given an <see cref="ApiConfig"/>.
    /// </summary>
    /// <remarks>
    /// This interface generally does not need to be implemented by a consuming application. The default implementation
    /// provided attempts to load the <see cref="IAuthentication"/> specified by <see cref="ApiConfig.AuthType"/>, injecting
    /// constructor dependencies, then supplying public properties from <see cref="ApiConfig.AuthProps"/>.
    /// </remarks>
    public interface IAuthenticationFactory
    {
        /// <summary>
        /// Build the <see cref="IAuthentication"/>.
        /// </summary>
        /// <param name="config">The <see cref="ApiConfig"/> to build an <see cref="IAuthentication"/> for.</param>
        /// <returns>The <see cref="IAuthentication"/></returns>
        IAuthentication BuildAuthentication(ApiConfig config);
    }
}