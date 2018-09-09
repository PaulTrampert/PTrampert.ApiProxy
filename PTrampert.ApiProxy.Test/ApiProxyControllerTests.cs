using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using PTrampert.ApiProxy.Exceptions;

namespace PTrampert.ApiProxy.Test
{
    public class ApiProxyControllerTests
    {
        private ApiProxyController subject;
        private ApiProxyConfig proxyConfig;
        private TestHttpHandler messageHandler;
        private Mock<IAuthenticationBuilder> authBuilder;

        [SetUp]
        public void SetUp()
        {
            messageHandler = new TestHttpHandler();
            messageHandler.NextResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
            var httpClient = new HttpClient(messageHandler);
            proxyConfig = new ApiProxyConfig();
            var proxyConfigOpts = new Mock<IOptions<ApiProxyConfig>>();
            proxyConfigOpts.SetupGet(o => o.Value).Returns(proxyConfig);
            authBuilder = new Mock<IAuthenticationBuilder>();
            subject = new ApiProxyController(httpClient, proxyConfigOpts.Object, authBuilder.Object);
            subject.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Test]
        public async Task ItThrowsProxyExceptionWhenApiNotConfigured()
        {
            try
            {
                await subject.Proxy("fake", "not/real/path");
                Assert.Fail("Should have thrown ProxyException");
            }
            catch (ProxyException e)
            {
                Assert.That(e.Status, Is.EqualTo((int)HttpStatusCode.NotFound));
                Assert.That(e.Message, Is.EqualTo($"No API named 'fake' configured."));
            }
        }

        [TestCase("GET", "some/path", "?some=value", null, null)]
        [TestCase("GET", "some/path", null, null, null)]
        [TestCase("POST", "some/path", null, "something", "text/plain")]
        [TestCase("PUT", "herp/derp/flerp", "?herp=derp", "something", "text/plain")]
        public async Task ItCallsTheRequestedApi(string method, string path, string query, string body, string contentType)
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com"
            });
            subject.Request.Method = method;
            subject.Request.Body = body == null ? Stream.Null : new MemoryStream(Encoding.UTF8.GetBytes(body));
            subject.Request.ContentLength = subject.Request.Body?.Length;
            subject.Request.ContentType = contentType;
            subject.Request.QueryString = new QueryString(query);

            await subject.Proxy("fake", path);

            var lastRequest = messageHandler.LastRequest;
            var lastRequestBody = lastRequest.Content == null ? null : await lastRequest.Content.ReadAsStringAsync();
            Assert.That(lastRequest.RequestUri.ToString(), Is.EqualTo($"https://example.com/{path}{query}"));
            Assert.That(lastRequestBody, Is.EqualTo(body));
            Assert.That(lastRequest.Content?.Headers.ContentType.MediaType, Is.EqualTo(contentType));
        }

        [TestCase(HttpStatusCode.OK, "somebody", "text/plain", "herp=derp&bloop=floop")]
        [TestCase(HttpStatusCode.InternalServerError, "somebody", "text/plain", "herp=derp&bloop=floop")]
        [TestCase(HttpStatusCode.NoContent, null, null, null)]
        public async Task ItSetsTheResponseOnTheController(HttpStatusCode code, string body, string contentType, string headers)
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com"
            });
            subject.Request.Method = "GET";
            messageHandler.NextResponse = new HttpResponseMessage(code);
            messageHandler.NextResponse.Content = body == null ? null : new StringContent(body, Encoding.UTF8, contentType);
            foreach (var pair in headers?.Split('&') ?? new string[0])
            {
                var kv = pair.Split('=');
                messageHandler.NextResponse.Headers.Add(kv[0], kv[1]);
            }

            await subject.Proxy("fake", "path");

            Assert.That(subject.Response.StatusCode, Is.EqualTo((int)code));
            if (body != null)
            {
                var responseBody = await new StreamReader(subject.Response.Body).ReadToEndAsync();
                Assert.That(responseBody, Is.EqualTo(body));
                Assert.That(subject.Response.ContentType.StartsWith(contentType));
            }

            if (headers != null)
            {
                foreach (var pair in headers.Split('&'))
                {
                    var kv = pair.Split('=');
                    Assert.That(subject.Response.Headers[kv[0]].ToString(), Is.EqualTo(kv[1]));
                }
            }
        }
        
        [Test]
        public async Task ItAddsAnAuthHeaderIfAuthBuilderReturnsAnAuthentication()
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com"
            });
            subject.Request.Method = "GET";
            var auth = new Mock<IAuthentication>();
            var authHeader = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("id:secret")));
            auth.Setup(a => a.GetAuthenticationHeader()).ReturnsAsync(authHeader);
            authBuilder.Setup(ab => ab.BuildAuthentication(proxyConfig["fake"])).Returns(auth.Object);

            await subject.Proxy("fake", "some/path");

            Assert.That(messageHandler.LastRequest.Headers.Authorization, Is.SameAs(authHeader));
        }

        [Test]
        public async Task ItReturnsTheResponse()
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com"
            });
            subject.Request.Method = "GET";
            var auth = new Mock<IAuthentication>();
            var authHeader = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("id:secret")));
            auth.Setup(a => a.GetAuthenticationHeader()).ReturnsAsync(authHeader);
            authBuilder.Setup(ab => ab.BuildAuthentication(proxyConfig["fake"])).Returns(auth.Object);

            var result = await subject.Proxy("fake", "some/path");

            Assert.That(result, Is.SameAs(subject.Response));
        }
    }
}
