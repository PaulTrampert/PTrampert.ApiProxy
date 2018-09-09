using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using PTrampert.ApiProxy.Authentication;

namespace PTrampert.ApiProxy.Test.Authentication
{
    public class UserBearerAuthenticationTests
    {
        private UserBearerAuthentication subject;
        private HttpContext httpContext;
        public Mock<IHttpContextAccessor> httpAccessor;
        public Mock<IAuthenticationService> authService;

        [SetUp]
        public void SetUp()
        {
            authService = new Mock<IAuthenticationService>();
            httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceCollection()
                    .AddSingleton(authService.Object)
                    .BuildServiceProvider()
            };
            httpAccessor = new Mock<IHttpContextAccessor>();
            httpAccessor.SetupGet(ha => ha.HttpContext).Returns(httpContext);
            subject = new UserBearerAuthentication(httpAccessor.Object);
        }

        [Test]
        public void ItSetsDefaultProps()
        {
            Assert.That(subject.Mode, Is.EqualTo(TokenMode.AuthProps.ToString()));
            Assert.That(subject.TokenKey, Is.EqualTo("access_token"));
        }

        [Test]
        public async Task GetAuthenticationHeaderReturnsTokenFromClaims()
        {
            var identity = new ClaimsIdentity(new[] {new Claim(subject.TokenKey, "token")});
            httpContext.User = new ClaimsPrincipal(identity);

            subject.Mode = TokenMode.Claims.ToString();

            var result = await subject.GetAuthenticationHeader();

            Assert.That(result.Scheme, Is.EqualTo("Bearer"));
            Assert.That(result.Parameter, Is.EqualTo("token"));
        }

        [Test]
        public async Task GetAuthenticationHeaderReturnsTokenFromCookieProps()
        {
            authService.Setup(a => a.AuthenticateAsync(httpContext, It.IsAny<string>()))
                .ReturnsAsync(
                    AuthenticateResult.Success(
                        new AuthenticationTicket(
                            new ClaimsPrincipal(),
                            new AuthenticationProperties(new Dictionary<string,string>
                            {
                                { $".Token.{subject.TokenKey}", "token" }
                            }),
                            "Cookies"
                        )
                    )
                );

            var result = await subject.GetAuthenticationHeader();

            Assert.That(result.Scheme, Is.EqualTo("Bearer"));
            Assert.That(result.Parameter, Is.EqualTo("token"));
        }
    }
}