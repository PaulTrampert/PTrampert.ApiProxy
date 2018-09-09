using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PTrampert.ApiProxy.Authentication;
using PTrampert.ApiProxy.Exceptions;

namespace PTrampert.ApiProxy.Test
{
    public class AuthenticationBuilderTests
    {
        private AuthenticationBuilder subject;
        private IServiceCollection serviceCollection;
        private IServiceProvider serviceProvider;
        private ApiConfig config;

        [SetUp]
        public void SetUp()
        {
            config = new ApiConfig
            {
                BaseUrl = "https://example.com"
            };
            serviceCollection = new ServiceCollection();
            serviceProvider = serviceCollection.BuildServiceProvider();
            subject = new AuthenticationBuilder(serviceProvider);
        }

        [Test]
        public void BuildAuthenticationReturnsNullWhenAuthTypeIsNull()
        {
            var result = subject.BuildAuthentication(config);
            Assert.IsNull(result);
        }

        [Test]
        public void BuildAuthenticationThrowsTypeNotFoundExceptionWhenTypeIsNotFound()
        {
            config.AuthType = "herpderp";
            try
            {
                subject.BuildAuthentication(config);
                Assert.Fail("Call should have thrown");
            }
            catch (TypeNotFoundException e)
            {
                Assert.That(e.Message, Is.EqualTo($"No type matching {config.AuthType} was found."));
            }
        }

        [Test]
        public void BuildAuthenticationThrowsInvalidTypeExceptionWhenTypeIsNotIAuthentication()
        {
            config.AuthType = "System.Object";
            try
            {
                subject.BuildAuthentication(config);
                Assert.Fail("Call should have thrown");
            }
            catch (InvalidAuthenticationTypeException e)
            {
                Assert.That(e.Message,
                    Is.EqualTo($"System.Object does not implement {typeof(IAuthentication).FullName}"));
            }
        }

        [Test]
        public void BuildAuthenticationBuildsAuthenticationIfAllIsWell()
        {
            config.AuthType = "PTrampert.ApiProxy.Authentication.BasicAuthentication";
            config.AuthProps = new Dictionary<string, string>
            {
                { "Id", "id"},
                { "Secret", "secret"}
            };

            var result = subject.BuildAuthentication(config) as BasicAuthentication;

            Assert.That(result.Id, Is.EqualTo("id"));
            Assert.That(result.Secret, Is.EqualTo("secret"));
        }
    }
}