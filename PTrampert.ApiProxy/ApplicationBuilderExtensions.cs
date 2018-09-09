using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseApiProxy(this IApplicationBuilder appBuilder, string basePath = "api")
        {
            appBuilder.UseMvc(routes =>
            {
                routes.MapRoute("PTrampert.ApiProxy", $"{basePath.TrimEnd('/')}/{{api}}/{{*path}}",
                    new {controller = "ApiProxy", action = "Proxy"});
            });
            return appBuilder;
        }
    }
}
