// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides an extension method to add ApiProxy to the request pipeline.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds ApiProxy to the ASP.NET Core request pipeline with the specified base path.
        /// </summary>
        /// <param name="appBuilder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="basePath">The base path to use for the api proxy. Defaults to <value>"api"</value>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/></returns>
        public static IApplicationBuilder UseApiProxy(this IApplicationBuilder appBuilder, string basePath = "api")
        {
#if NETSTANDARD2_0
            appBuilder.UseMvc(routes =>
            {
                routes.MapRoute("PTrampert.ApiProxy", $"{basePath.TrimEnd('/')}/{{api}}/{{*path}}",
                    new {controller = "ApiProxy", action = "Proxy"});
            });
#else
            appBuilder.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("PTrampert.ApiProxy", $"{basePath.TrimEnd('/')}/{{api}}/{{*path}}",
                    new {controller = "ApiProxy", action = "Proxy"});
            });
#endif
            return appBuilder;
        }
    }
}
