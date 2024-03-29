﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using PTrampert.ApiProxy.Authentication;
using PTrampert.ApiProxy.Exceptions;

namespace PTrampert.ApiProxy.Test
{
    public class DefaultAuthenticationFactoryTests
    {
        private DefaultAuthenticationFactory subject;
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
            subject = new DefaultAuthenticationFactory(serviceProvider);
        }

        [Test]
        public void BuildAuthenticationReturnsNullWhenAuthTypeIsNull()
        {
            var result = subject.BuildAuthentication(config);
            Assert.That(result, Is.Null);
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

        [Test]
        public void CanBuildBearerAuthentication()
        {
            var httpAccessor = new Mock<IHttpContextAccessor>();
            serviceCollection.AddSingleton(httpAccessor.Object);
            serviceProvider = serviceCollection.BuildServiceProvider();
            subject = new DefaultAuthenticationFactory(serviceProvider);
            config.AuthType = typeof(UserBearerAuthentication).FullName;
            config.AuthProps = new Dictionary<string, string>
            {
                { "Mode", "Claims" },
                { "TokenKey", "token" }
            };

            var result = subject.BuildAuthentication(config) as UserBearerAuthentication;

            Assert.That(result.Mode, Is.EqualTo(TokenMode.Claims.ToString()));
            Assert.That(result.TokenKey, Is.EqualTo("token"));
        }
    }
}