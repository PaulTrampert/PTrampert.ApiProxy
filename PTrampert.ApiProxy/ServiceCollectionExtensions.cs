using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PTrampert.ApiProxy;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extension methods to <see cref="IServiceCollection"/> to register required dependencies for the api proxy.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configure api proxy using an <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/></param>
        /// <param name="config">The <see cref="IConfiguration"/> who's keys should map to <see cref="ApiProxyConfig"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddApiProxy(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<ApiProxyConfig>(config);
            return Common(services);
        }

        /// <summary>
        /// Configure api proxy using a setup action.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/></param>
        /// <param name="setupAction">The <see cref="Action{ApiProxyConfig}"/> that sets the configuration properties on an <see cref="ApiProxyConfig"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddApiProxy(this IServiceCollection services, Action<ApiProxyConfig> setupAction)
        {
            services.Configure(setupAction);
            return Common(services);
        }

        private static IServiceCollection Common(IServiceCollection services)
        {
#if NETCOREAPP3_0
            services.AddControllers();
#endif
            services.TryAddScoped<IAuthenticationFactory, DefaultAuthenticationFactory>();
            services.TryAddSingleton<HttpClient>();
            return services;
        }
    }
}
