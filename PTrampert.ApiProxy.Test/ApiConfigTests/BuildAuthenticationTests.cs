using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using PTrampert.ApiProxy.Authentication;
using PTrampert.ApiProxy.Exceptions;

namespace PTrampert.ApiProxy.Test.ApiConfigTests
{
    public class BuildAuthenticationTests
    {
        private ApiConfig config;

        [SetUp]
        public void SetUp()
        {
            config = new ApiConfig
            {
                BaseUrl = "https://example.com",
                AuthType = "PTrampert.ApiProxy.Authentication.BasicAuthentication",
                AuthArgs = new Dictionary<string, string>
                {
                    { "id", "someId" },
                    { "secret", "somesecret" }
                }
            };
        }

        [Test]
        public void ItReturnsNullIfNoAuthTypeIsGiven()
        {
            config.AuthType = null;
            var result = config.BuildAuthentication();
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ItCreatesTheIAuthenticationIfEverythingIsFine()
        {
            var result = config.BuildAuthentication();
            Assert.That(result, Is.InstanceOf<BasicAuthentication>());
        }

        [Test]
        public void ItThrowsTypeNotFoundExceptionIfTypeIsNotFound()
        {
            config.AuthType = "SomeUnknownType";
            try
            {
                config.BuildAuthentication();
            }
            catch (TypeNotFoundException e)
            {
                Assert.That(e.Message, Is.EqualTo($"No type matching {config.AuthType} was found."));
            }
        }

        [Test]
        public void ItThrowsInvalidAuthenticationTypeExceptionIfTheTypeIsntIAuthentication()
        {
            config.AuthType = "System.Object";
            try
            {
                config.BuildAuthentication();
            }
            catch (InvalidAuthenticationTypeException e)
            {
                Assert.That(e.Message, Is.EqualTo($"System.Object does not implement {typeof(IAuthentication).FullName}"));
            }
        }

        [Test]
        public void ItThrowsConstructorNotFoundExceptionWhenExtraArgsAreGiven()
        {
            config.AuthArgs.Add("herp", "derp");
            try
            {
                config.BuildAuthentication();
            }
            catch (ConstructorNotFoundException e)
            {
                Assert.That(e.Message, Is.EqualTo($"No constructor found matching arguments with keys: {string.Join(", ", config.AuthArgs.Keys)}"));
            }
        }

        [Test]
        public void ItThrowsConstructorNotFoundExceptionWhenRequiredArgsAreMissing()
        {
            config.AuthArgs.Remove("secret");
            try
            {
                config.BuildAuthentication();
            }
            catch (ConstructorNotFoundException e)
            {
                Assert.That(e.Message, Is.EqualTo($"No constructor found matching arguments with keys: {string.Join(", ", config.AuthArgs.Keys)}"));
            }
        }
    }
}
