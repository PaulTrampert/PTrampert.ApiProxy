using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
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
        private HeaderDictionary requestHeaders;
        private HeaderDictionary responseHeaders;

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
            requestHeaders = new HeaderDictionary();
            httpRequest.SetupGet(r => r.Headers)
                .Returns(requestHeaders);
            httpContext.SetupGet(c => c.Request)
                .Returns(httpRequest.Object);
            webSockets = new Mock<WebSocketManager>();
            webSockets.SetupGet(ws => ws.IsWebSocketRequest)
                .Returns(false);
            httpContext.SetupGet(c => c.WebSockets)
                .Returns(webSockets.Object);
            responseHeaders = new HeaderDictionary();
            httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupAllProperties();
            httpResponse.SetupGet(r => r.Headers)
                .Returns(responseHeaders);
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

        [TestCase("text/plain; charset=utf-8")]
        [TestCase("application/json;charset=UTF-8")]
        [TestCase("bogus")]
        [TestCase("text/plain; charset=")]
        [TestCase("text/ plain")]
        public async Task ItForwardsTheIncomingContentTypeVerbatim(string contentType)
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com"
            });
            subject.Request.Method = "POST";
            subject.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("something"));
            subject.Request.ContentLength = subject.Request.Body.Length;
            subject.Request.ContentType = contentType;

            await subject.Proxy("fake", "some/path");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messageHandler.LastRequestBody, Is.EqualTo("something"));
                Assert.That(messageHandler.LastRequestRawContentType, Is.EqualTo(contentType));
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task ItSendsNoContentTypeWhenTheIncomingRequestHasNone(string contentType)
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com"
            });
            subject.Request.Method = "POST";
            subject.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("something"));
            subject.Request.ContentLength = subject.Request.Body.Length;
            subject.Request.ContentType = contentType;

            await subject.Proxy("fake", "some/path");

            Assert.That(messageHandler.LastRequestRawContentType, Is.Null);
        }

        [Test]
        public async Task ItProxiesConfiguredRequestHeadersFromTheIncomingRequest()
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                RequestHeaders = new List<string>
                {
                    "herp",
                    "bloop"
                }
            });
            subject.Request.Method = "GET";
            requestHeaders["herp"] = "derp";
            requestHeaders["bloop"] = "floop";

            await subject.Proxy("fake", "some/path");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messageHandler.LastRequestHeaders["herp"], Is.EqualTo(["derp"]));
                Assert.That(messageHandler.LastRequestHeaders["bloop"], Is.EqualTo(["floop"]));
            }
        }

        [Test]
        public async Task ItProxiesEveryValueOfAMultiValuedRequestHeader()
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                RequestHeaders = new List<string>
                {
                    "herp"
                }
            });
            subject.Request.Method = "GET";
            requestHeaders["herp"] = new StringValues(["derp", "flerp"]);

            await subject.Proxy("fake", "some/path");

            Assert.That(messageHandler.LastRequestHeaders["herp"], Is.EqualTo(["derp", "flerp"]));
        }

        [Test]
        public async Task ItProxiesRequestHeaderNamesExactlyAsTheyWereGiven()
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                RequestHeaders = new List<string>
                {
                    "herp-derp"
                }
            });
            subject.Request.Method = "GET";
            requestHeaders["HeRp-DeRp"] = "derp";

            await subject.Proxy("fake", "some/path");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messageHandler.LastRequestHeaders["HeRp-DeRp"], Is.EqualTo(["derp"]));
                Assert.That(messageHandler.LastRequestHeaders.ContainsKey("herp-derp"), Is.False);
            }
        }

        [TestCase("Mozilla/5.0 (Windows NT 10.0; Win64; x64")]
        [TestCase("MyApp/1.0 [build 5]")]
        public async Task ItProxiesRequestHeaderValuesThatWouldFailHeaderValidation(string userAgent)
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                RequestHeaders = new List<string>
                {
                    "User-Agent"
                }
            });
            subject.Request.Method = "GET";
            requestHeaders["User-Agent"] = userAgent;

            await subject.Proxy("fake", "some/path");

            Assert.That(messageHandler.LastRequestHeaders["User-Agent"], Is.EqualTo([userAgent]));
        }

        [Test]
        public async Task ItThrowsProxyExceptionWhenAConfiguredRequestHeaderCannotBeForwarded()
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                RequestHeaders = new List<string>
                {
                    "Content-Type"
                }
            });
            subject.Request.Method = "GET";
            requestHeaders["Content-Type"] = "text/plain";

            // The delegate is cast explicitly because the Func<Task> and AsyncTestDelegate overloads
            // of ThrowsAsync are otherwise ambiguous.
            var exception = await Assert.ThrowsAsync<ProxyException>((Func<Task>)(() => subject.Proxy("fake", "some/path")));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception?.Status, Is.EqualTo((int)HttpStatusCode.InternalServerError));
                Assert.That(exception?.Message, Does.Contain("Content-Type"));
            }
        }

        [Test]
        public async Task ItDoesNotProxyRequestHeadersThatAreNotConfigured()
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                RequestHeaders = new List<string>
                {
                    "herp"
                }
            });
            subject.Request.Method = "GET";
            requestHeaders["herp"] = "derp";
            requestHeaders["bloop"] = "floop";

            await subject.Proxy("fake", "some/path");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messageHandler.LastRequestHeaders.ContainsKey("herp"), Is.True);
                Assert.That(messageHandler.LastRequestHeaders.ContainsKey("bloop"), Is.False);
            }
        }

        [Test]
        public async Task ItSkipsConfiguredRequestHeadersMissingFromTheIncomingRequest()
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                RequestHeaders = new List<string>
                {
                    "herp",
                    "bloop"
                }
            });
            subject.Request.Method = "GET";
            requestHeaders["herp"] = "derp";

            await subject.Proxy("fake", "some/path");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messageHandler.LastRequestHeaders["herp"], Is.EqualTo(["derp"]));
                Assert.That(messageHandler.LastRequestHeaders.ContainsKey("bloop"), Is.False);
            }
        }

        [Test]
        public async Task ItProxiesResponseHeaderNamesExactlyAsTheyWereGiven()
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                ResponseHeaders = new List<string>
                {
                    "herp-derp"
                }
            });
            subject.Request.Method = "GET";
            messageHandler.NextResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
            messageHandler.NextResponse.Headers.Add("HeRp-DeRp", "derp");

            await subject.Proxy("fake", "some/path");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(responseHeaders.Keys, Does.Contain("HeRp-DeRp"));
                Assert.That(responseHeaders.Keys, Does.Not.Contain("herp-derp"));
                Assert.That(responseHeaders["HeRp-DeRp"], Is.EqualTo(new StringValues("derp")));
            }
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

            using (Assert.EnterMultipleScope())
            {
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
                        Assert.That(responseHeaders[kv[0]], Is.EqualTo(kv[1]));
                    }
                }
            }
        }
        
        [Test]
        public async Task ItStreamsTheUpstreamResponseBeforeTheUpstreamHasFinishedSending()
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com"
            });
            subject.Request.Method = "GET";
            // The pipe stands in for an upstream that is still sending: until the writer completes, a buffering
            // proxy would wait forever for the rest of the body.
            var upstream = new Pipe();
            messageHandler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(upstream.Reader.AsStream())
            };
            await upstream.Writer.WriteAsync(Encoding.UTF8.GetBytes("first"));

            var result = await subject.Proxy("fake", "some/path").WaitAsync(TimeSpan.FromSeconds(5));

            var fileStream = ((FileStreamResult)result).FileStream;
            var firstBytes = new byte[5];
            await fileStream.ReadExactlyAsync(firstBytes).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(Encoding.UTF8.GetString(firstBytes), Is.EqualTo("first"));

            await upstream.Writer.WriteAsync(Encoding.UTF8.GetBytes(" second"));
            await upstream.Writer.CompleteAsync();
            var rest = await new StreamReader(fileStream).ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(rest, Is.EqualTo(" second"));
        }

        [Test]
        public async Task ItProxiesAChunkedUpstreamResponseWithNoContentLengthIntact()
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com"
            });
            subject.Request.Method = "GET";
            var upstream = new Pipe();
            await upstream.Writer.WriteAsync(Encoding.UTF8.GetBytes("chunked body"));
            await upstream.Writer.CompleteAsync();
            var upstreamContent = new StreamContent(upstream.Reader.AsStream());
            upstreamContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            messageHandler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = upstreamContent
            };

            var result = await subject.Proxy("fake", "some/path");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(upstreamContent.Headers.ContentLength, Is.Null);
                Assert.That(result, Is.InstanceOf<FileStreamResult>());
                var fileStreamResult = (FileStreamResult)result;
                Assert.That(fileStreamResult.ContentType, Is.EqualTo("text/plain"));
                var content = await new StreamReader(fileStreamResult.FileStream).ReadToEndAsync();
                Assert.That(content, Is.EqualTo("chunked body"));
            }
        }

        [TestCase("GET", HttpStatusCode.NoContent)]
        [TestCase("GET", HttpStatusCode.NotModified)]
        [TestCase("HEAD", HttpStatusCode.OK)]
        public async Task ItReturnsAnEmptyResultWhenTheResponseHasNoBody(string method, HttpStatusCode code)
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com"
            });
            subject.Request.Method = method;
            // The content claims a length, as the upstream's headers would for a HEAD request, so emptiness
            // has to come from the method and status rather than from the length.
            var upstreamContent = new ByteArrayContent([]);
            upstreamContent.Headers.ContentLength = 1024;
            messageHandler.NextResponse = new HttpResponseMessage(code)
            {
                Content = upstreamContent
            };

            var result = await subject.Proxy("fake", "some/path");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Response.StatusCode, Is.EqualTo((int)code));
                Assert.That(result, Is.InstanceOf<EmptyResult>());
            }
        }

        [Test]
        public async Task ItDisposesTheUpstreamResponseOnceTheResponseHasBeenWritten()
        {
            proxyConfig.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com"
            });
            subject.Request.Method = "GET";
            var upstreamResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("somebody")
            };
            messageHandler.NextResponse = upstreamResponse;

            await subject.Proxy("fake", "some/path");

            httpResponse.Verify(r => r.RegisterForDispose(upstreamResponse));
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
        public async Task ItThrowsProxyExceptionIfApiNotConfiguredForWebSocketsAndWebSocketIsRequested()
        {
            var apiConfig = new ApiConfig
            {
                WsBaseUrl = null
            };
            proxyConfig.Add("fake", apiConfig);
            webSockets.SetupGet(ws => ws.IsWebSocketRequest)
                .Returns(true);

            // The delegate is cast explicitly because the Func<Task> and AsyncTestDelegate overloads
            // of ThrowsAsync are otherwise ambiguous.
            var exception = await Assert.ThrowsAsync<ProxyException>((Func<Task>)(() => subject.Proxy("fake", "some/path")));
            Assert.That(exception?.Status, Is.EqualTo((int)HttpStatusCode.BadRequest));
        }
    }
}
