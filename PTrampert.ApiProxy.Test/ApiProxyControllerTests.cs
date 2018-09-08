using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace PTrampert.ApiProxy.Test
{
    public class ApiProxyControllerTests
    {
        private ApiProxyController subject;
        private ApiProxyConfig proxyConfig;

        [SetUp]
        public void SetUp()
        {
            var httpClient = new HttpClient();
            proxyConfig = new ApiProxyConfig();
            var proxyConfigOpts = new Mock<IOptions<ApiProxyConfig>>();
            proxyConfigOpts.SetupGet(o => o.Value).Returns(proxyConfig);
            subject = new ApiProxyController(httpClient, proxyConfigOpts.Object);
        }

        [Test]
        public async Task ItReturnsNotFoundIfApiIsNotFound()
        {
            var result = await subject.Proxy("fake", "not/real/path");
            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
            var notFoundResult = (NotFoundObjectResult) result;
            Assert.That(notFoundResult.Value, Is.InstanceOf<ApiError>());
            var apiError = (ApiError)notFoundResult.Value;
            Assert.That(apiError.Message, Is.EqualTo("Unknown api: fake"));
        }
    }
}
