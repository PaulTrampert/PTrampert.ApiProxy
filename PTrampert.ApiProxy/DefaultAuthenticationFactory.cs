using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PTrampert.ApiProxy.Exceptions;

namespace PTrampert.ApiProxy
{
    /// <summary>
    /// The default implementation of <see cref="IAuthenticationFactory"/>.
    /// </summary>
    public class DefaultAuthenticationFactory : IAuthenticationFactory
    {
        private readonly IServiceProvider services;

        /// <summary>
        /// Constructor for <see cref="DefaultAuthenticationFactory"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceProvider"/> used to resolve constructor dependencies.</param>
        public DefaultAuthenticationFactory(IServiceProvider services)
        {
            this.services = services;
        }

        /// <summary>
        /// Builds the <see cref="IAuthentication"/> specified by <see cref="ApiConfig"/>.
        /// </summary>
        /// <param name="config">The <see cref="ApiConfig"/></param>
        /// <returns>The <see cref="IAuthentication"/>.</returns>
        public IAuthentication BuildAuthentication(ApiConfig config)
        {
            if (config.AuthType == null)
            {
                return null;
            }
            var authType = Type.GetType(config.AuthType);
            if (authType == null)
            {
                throw new TypeNotFoundException(config.AuthType);
            }
            if (!typeof(IAuthentication).IsAssignableFrom(authType))
            {
                throw new InvalidAuthenticationTypeException(authType);
            }

            var authentication = (IAuthentication)ActivatorUtilities.CreateInstance(services, authType);
            var props = authType.GetProperties();
            foreach (var prop in props.Where(p => p.SetMethod.IsPublic))
            {
                if (config.AuthProps.ContainsKey(prop.Name))
                {
                    prop.SetValue(authentication, config.AuthProps[prop.Name]);
                }
            }

            return authentication;
        }
    }
}
