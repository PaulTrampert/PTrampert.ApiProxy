﻿using System;
using System.Collections.Generic;
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
        private Mock<IAuthenticationFactory> authBuilder;
        private Mock<IWebSocketProxy> webSocketProxy;
        private Mock<HttpContext> httpContext;
        private Mock<HttpRequest> httpRequest;
        private Mock<WebSocketManager> webSockets;
        private Mock<HttpResponse> httpResponse;
        private Mock<IHeaderDictionary> responseHeaders;

        [SetUp]
        public void SetUp()
        {
            messageHandler = new TestHttpHandler();
            messageHandler.NextResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
            var httpClient = new HttpClient(messageHandler);
            proxyConfig = new ApiProxyConfig();
            var proxyConfigOpts = new Mock<IOptions<ApiProxyConfig>>();
            proxyConfigOpts.SetupGet(o => o.Value).Returns(proxyConfig);
            authBuilder = new Mock<IAuthenticationFactory>();
            webSocketProxy = new Mock<IWebSocketProxy>();
            httpContext = new Mock<HttpContext>();
            httpRequest = new Mock<HttpRequest>();
            httpRequest.SetupAllProperties();
            httpContext.SetupGet(c => c.Request)
                .Returns(httpRequest.Object);
            webSockets = new Mock<WebSocketManager>();
            webSockets.SetupGet(ws => ws.IsWebSocketRequest)
                .Returns(false);
            httpContext.SetupGet(c => c.WebSockets)
                .Returns(webSockets.Object);
            responseHeaders = new Mock<IHeaderDictionary>();
            responseHeaders.SetupAllProperties();
            httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupAllProperties();
            httpResponse.SetupGet(r => r.Headers)
                .Returns(responseHeaders.Object);
            httpContext.SetupGet(c => c.Response)
                .Returns(httpResponse.Object);
            subject = new ApiProxyController(httpClient, proxyConfigOpts.Object, authBuilder.Object, webSocketProxy.Object);
            subject.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext.Object
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

            Assert.That(messageHandler.LastRequestUrl, Is.EqualTo($"https://example.com/{path}{query}"));
            Assert.That(messageHandler.LastRequestBody, Is.EqualTo(body));
            Assert.That(messageHandler.LastRequestMediaType, Is.EqualTo(contentType));
        }

        [TestCase(HttpStatusCode.OK, "somebody", "text/plain", "herp=derp&bloop=floop")]
        [TestCase(HttpStatusCode.InternalServerError, "somebody", "text/plain", "herp=derp&bloop=floop")]
        [TestCase(HttpStatusCode.NoContent, null, null, null)]
        public async Task ItSetsTheResponseOnTheController(HttpStatusCode code, string body, string contentType, string headers)
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                ResponseHeaders = new List<string>
                {
                    "herp",
                    "bloop"
                }
            });
            subject.Request.Method = "GET";
            messageHandler.NextResponse = new HttpResponseMessage(code);
            messageHandler.NextResponse.Content = body == null ? null : new StringContent(body, Encoding.UTF8, contentType);
            foreach (var pair in headers?.Split('&') ?? new string[0])
            {
                var kv = pair.Split('=');
                messageHandler.NextResponse.Headers.Add(kv[0], kv[1]);
            }

            var result = await subject.Proxy("fake", "path");

            Assert.That(subject.Response.StatusCode, Is.EqualTo((int)code));
            if (body != null)
            {
                var fileStreamResult = result as FileStreamResult;
                Assert.That(fileStreamResult.ContentType.StartsWith(contentType));
                var content = await new StreamReader(fileStreamResult.FileStream).ReadToEndAsync();
                Assert.That(content, Is.EqualTo(body));
            }
            else
            {
                Assert.That(result, Is.InstanceOf<EmptyResult>());
            }

            if (headers != null)
            {
                foreach (var pair in headers.Split('&'))
                {
                    var kv = pair.Split('=');
                    responseHeaders.Verify(h => h.Add(kv[0], kv[1]));
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
            httpRequest.SetupGet(r => r.Method)
                .Returns("GET");
            var auth = new Mock<IAuthentication>();
            var authHeader = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("id:secret")));
            auth.Setup(a => a.GetAuthenticationHeader()).ReturnsAsync(authHeader);
            authBuilder.Setup(ab => ab.BuildAuthentication(proxyConfig["fake"])).Returns(auth.Object);

            await subject.Proxy("fake", "some/path");

            Assert.That(messageHandler.LastRequestAuthenticationHeader, Is.SameAs(authHeader));
        }

        [Test]
        public async Task ItProxiesWebSocketConnectionsWhenRequested()
        {
            var apiConfig = new ApiConfig
            {
                WsBaseUrl = "ws://example.com"
            };
            proxyConfig.Add("fake", apiConfig);
            webSockets.SetupGet(ws => ws.IsWebSocketRequest)
                .Returns(true);

            var result = await subject.Proxy("fake", "some/path");
            
            webSocketProxy.Verify(wsp => wsp.Proxy(httpContext.Object, apiConfig, "some/path"));
            Assert.That(result, Is.TypeOf<EmptyResult>());
        }

        [Test]
        public void ItThrowsProxyExceptionIfApiNotConfiguredForWebSocketsAndWebSocketIsRequested()
        {
            var apiConfig = new ApiConfig
            {
                WsBaseUrl = null
            };
            proxyConfig.Add("fake", apiConfig);
            webSockets.SetupGet(ws => ws.IsWebSocketRequest)
                .Returns(true);

            var exception = Assert.ThrowsAsync<ProxyException>(async () => await subject.Proxy("fake", "some/path"));
            Assert.That(exception?.Status, Is.EqualTo((int)HttpStatusCode.BadRequest));
        }
    }
}
