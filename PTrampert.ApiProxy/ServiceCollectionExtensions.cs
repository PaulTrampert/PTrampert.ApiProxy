using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PTrampert.ApiProxy;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiProxy(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<ApiProxyConfig>(config);
            return Common(services);
        }

        public static IServiceCollection AddApiProxy(this IServiceCollection services, Action<ApiProxyConfig> setupAction)
        {
            services.Configure<ApiProxyConfig>(setupAction);
            return Common(services);
        }

        private static IServiceCollection Common(IServiceCollection services)
        {
            services.TryAddScoped<IAuthenticationBuilder, AuthenticationBuilder>();
            services.TryAddSingleton<HttpClient>();
            return services;
        }
    }
}
