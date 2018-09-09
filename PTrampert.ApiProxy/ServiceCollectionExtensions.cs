using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Configuration;
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
            services.AddScoped<IAuthenticationBuilder, AuthenticationBuilder>();
            if (!services.Any(sd => typeof(HttpClient).IsAssignableFrom(sd.ServiceType)))
            {
                services.AddSingleton<HttpClient>();
            }

            return services;
        }
    }
}
