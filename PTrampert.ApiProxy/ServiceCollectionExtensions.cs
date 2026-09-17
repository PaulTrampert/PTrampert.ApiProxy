using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
            // Registered as an enumerable so that a consumer's own IValidateOptions<ApiProxyConfig> is kept too,
            // and validated on start so that a reserved header surfaces before any traffic reaches the proxy.
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<ApiProxyConfig>, ApiProxyConfigValidator>());
            services.AddOptions<ApiProxyConfig>().ValidateOnStart();
            services.AddControllers();
            services.AddScoped<IWebSocketProxy, WebSocketProxy>();
            services.TryAddScoped<IAuthenticationFactory, DefaultAuthenticationFactory>();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHttpClient();
            return services;
        }
    }
}
