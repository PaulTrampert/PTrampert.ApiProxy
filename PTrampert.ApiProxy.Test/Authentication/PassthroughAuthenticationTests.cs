using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using PTrampert.ApiProxy.Authentication;
using PTrampert.ApiProxy.Exceptions;

namespace PTrampert.ApiProxy.Test.Authentication
{
    public class PassthroughAuthenticationTests
    {
        private PassthroughAuthentication subject;
        private HttpContext httpContext;
        private Mock<IHttpContextAccessor> httpAccessor;

        [SetUp]
        public void SetUp()
        {
            httpContext = new DefaultHttpContext();
            httpAccessor = new Mock<IHttpContextAccessor>();
            httpAccessor.SetupGet(ha => ha.HttpContext).Returns(() => httpContext);
            subject = new PassthroughAuthentication(httpAccessor.Object);
        }

        [TestCase("Bearer some.jwt.token", "Bearer", "some.jwt.token")]
        [TestCase("Basic dXNlcjpwYXNz", "Basic", "dXNlcjpwYXNz")]
        [TestCase("Custom a=\"b\", c=d", "Custom", "a=\"b\", c=d")]
        public async Task ItReturnsTheIncomingAuthorizationHeader(string incoming, string scheme, string parameter)
        {
            httpContext.Request.Headers.Authorization = incoming;

            var result = await subject.GetAuthenticationHeader();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Scheme, Is.EqualTo(scheme));
                Assert.That(result.Parameter, Is.EqualTo(parameter));
                Assert.That(result.ToString(), Is.EqualTo(incoming));
            }
        }

        [Test]
        public async Task ItReturnsASchemeOnlyHeaderWhenTheIncomingHeaderHasNoParameter()
        {
            httpContext.Request.Headers.Authorization = "Negotiate";

            var result = await subject.GetAuthenticationHeader();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Scheme, Is.EqualTo("Negotiate"));
                Assert.That(result.Parameter, Is.Null);
            }
        }

        [Test]
        public async Task ItReturnsNullWhenTheIncomingRequestHasNoAuthorizationHeader()
        {
            var result = await subject.GetAuthenticationHeader();

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task ItReturnsNullWhenThereIsNoHttpContext()
        {
            httpContext = null;

            var result = await subject.GetAuthenticationHeader();

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task ItThrowsABadRequestProxyExceptionWhenTheIncomingSchemeIsInvalid()
        {
            httpContext.Request.Headers.Authorization = "Bad(Scheme) token";

            // The delegate is cast explicitly because the Func<Task> and AsyncTestDelegate overloads of ThrowsAsync
            // are otherwise ambiguous when building for net8.0.
            var exception = await Assert.ThrowsAsync<ProxyException>((Func<Task>)(() => subject.GetAuthenticationHeader()));

            Assert.That(exception?.Status, Is.EqualTo(400));
        }
    }
}
