using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PTrampert.ApiProxy.Exceptions;

namespace PTrampert.ApiProxy
{
    internal class AuthenticationBuilder
    {
        private readonly IServiceProvider services;

        public AuthenticationBuilder(IServiceProvider services)
        {
            this.services = services;
        }

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
