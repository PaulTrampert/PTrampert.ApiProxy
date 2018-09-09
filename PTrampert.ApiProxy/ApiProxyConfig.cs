using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using PTrampert.ApiProxy.Exceptions;

namespace PTrampert.ApiProxy
{
    public class ApiProxyConfig : Dictionary<string, ApiConfig>
    {
    }

    public class ApiConfig
    {
        private string baseUrl;

        public string BaseUrl
        {
            get => baseUrl.TrimEnd('/');
            set => baseUrl = value;
        }

        public string AuthType { get; set; }

        public IDictionary<string, string> AuthArgs { get; set; }

        internal IAuthentication BuildAuthentication()
        {
            if (AuthType == null)
            {
                return null;
            }
            var authType = Type.GetType(AuthType);
            if (authType == null)
            {
                throw new TypeNotFoundException(AuthType);
            }
            if (!typeof(IAuthentication).IsAssignableFrom(authType))
            {
                throw new InvalidAuthenticationTypeException(authType);
            }
            var constructor = authType.GetConstructors().SingleOrDefault(ParametersMatchArgs);
            if (constructor == null)
            {
                throw new ConstructorNotFoundException(AuthArgs);
            }

            var args = SortArgs(constructor.GetParameters());
            var authentication = constructor.Invoke(args) as IAuthentication;
            return authentication;
        }

        private object[] SortArgs(ParameterInfo[] parameters)
        {
            var args = new List<object>();
            foreach (var param in parameters)
            {
                args.Add(AuthArgs[param.Name]);
            }

            return args.ToArray();
        }

        private bool ParametersMatchArgs(ConstructorInfo ctor)
        {
            var parameters = ctor.GetParameters();
            return AuthArgs.Keys.All(k => parameters.Any(p => p.Name == k)) && parameters.All(p => AuthArgs.Keys.Contains(p.Name));
        }
    }
}
