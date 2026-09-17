using System.Collections.Generic;
using System.Net.Http;
using NUnit.Framework;
using PTrampert.ApiProxy.Authentication;

namespace PTrampert.ApiProxy.Test
{
    public class ApiProxyConfigValidatorTests
    {
        private ApiProxyConfigValidator subject;
        private ApiProxyConfig config;

        [SetUp]
        public void SetUp()
        {
            subject = new ApiProxyConfigValidator();
            config = new ApiProxyConfig();
        }

        [Test]
        public void ItSucceedsForAConfigWithNoReservedHeaders()
        {
            config.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                RequestHeaders = new List<string> { "X-Some-Header", "User-Agent" },
                ResponseHeaders = new List<string> { "Link", "X-Some-Header" }
            });

            var result = subject.Validate(null, config);

            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public void ItSucceedsForAConfigWithNoHeadersAtAll()
        {
            config.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com"
            });

            var result = subject.Validate(null, config);

            Assert.That(result.Succeeded, Is.True);
        }

        [TestCase("Content-Type")]
        [TestCase("content-length")]
        [TestCase("Content-Disposition")]
        [TestCase("Allow")]
        [TestCase("EXPIRES")]
        [TestCase("Last-Modified")]
        public void ItFailsWhenAContentHeaderIsConfiguredAsARequestHeader(string header)
        {
            config.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                RequestHeaders = new List<string> { header }
            });

            var result = subject.Validate(null, config);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Failed, Is.True);
                Assert.That(result.FailureMessage, Does.Contain(header).And.Contain("fake").And.Contain("RequestHeaders"));
            }
        }

        [TestCase("Authorization")]
        [TestCase("authorization")]
        public void ItFailsWhenAuthorizationIsConfiguredAsARequestHeaderAlongsideAnAuthType(string header)
        {
            config.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                AuthType = typeof(BasicAuthentication).AssemblyQualifiedName,
                RequestHeaders = new List<string> { header }
            });

            var result = subject.Validate(null, config);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Failed, Is.True);
                Assert.That(result.FailureMessage, Does.Contain(header).And.Contain("fake").And.Contain("AuthType"));
            }
        }

        // Without an AuthType the proxy sets no Authorization header of its own, so an api can legitimately
        // forward the client's. Reserving the header unconditionally would break those configurations.
        [TestCase("Authorization")]
        [TestCase("authorization")]
        public void ItSucceedsWhenAuthorizationIsConfiguredAsARequestHeaderWithoutAnAuthType(string header)
        {
            config.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                RequestHeaders = new List<string> { header }
            });

            var result = subject.Validate(null, config);

            Assert.That(result.Succeeded, Is.True);
        }

        [TestCase("Content-Type")]
        [TestCase("content-length")]
        [TestCase("Last-Modified")]
        public void ItFailsWhenAContentHeaderIsConfiguredAsAResponseHeader(string header)
        {
            config.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                ResponseHeaders = new List<string> { header }
            });

            var result = subject.Validate(null, config);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Failed, Is.True);
                Assert.That(result.FailureMessage, Does.Contain(header).And.Contain("fake").And.Contain("ResponseHeaders"));
            }
        }

        [Test]
        public void ItSucceedsWhenAuthorizationIsConfiguredAsAResponseHeader()
        {
            config.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                ResponseHeaders = new List<string> { "Authorization" }
            });

            var result = subject.Validate(null, config);

            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public void ItReportsEveryReservedHeaderAcrossEveryApi()
        {
            config.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                RequestHeaders = new List<string> { "Content-Type", "X-Fine" }
            });
            config.Add("other", new ApiConfig
            {
                BaseUrl = "https://example.org",
                ResponseHeaders = new List<string> { "Content-Length" }
            });

            var result = subject.Validate(null, config);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Failures, Has.Exactly(2).Items);
                Assert.That(result.FailureMessage, Does.Contain("fake").And.Contain("Content-Type"));
                Assert.That(result.FailureMessage, Does.Contain("other").And.Contain("Content-Length"));
            }
        }

        // The reserved content headers are not a hand-written guess: they are the set System.Net.Http keeps on
        // HttpContent.Headers. This pins the list to what HttpClient actually does, so a name that stops (or
        // starts) being refused on a request message shows up here rather than in production.
        [TestCase("Allow")]
        [TestCase("Content-Disposition")]
        [TestCase("Content-Encoding")]
        [TestCase("Content-Language")]
        [TestCase("Content-Length")]
        [TestCase("Content-Location")]
        [TestCase("Content-MD5")]
        [TestCase("Content-Range")]
        [TestCase("Content-Type")]
        [TestCase("Expires")]
        [TestCase("Last-Modified")]
        [TestCase("User-Agent")]
        [TestCase("Accept")]
        [TestCase("Authorization")]
        [TestCase("Cache-Control")]
        [TestCase("ETag")]
        [TestCase("Link")]
        [TestCase("Location")]
        [TestCase("Retry-After")]
        [TestCase("Set-Cookie")]
        [TestCase("Vary")]
        [TestCase("Age")]
        [TestCase("Date")]
        [TestCase("Warning")]
        [TestCase("Content-Security-Policy")]
        [TestCase("Access-Control-Allow-Origin")]
        [TestCase("X-Custom")]
        public void ItReservesARequestHeaderExactlyWhenAnUpstreamRequestCannotCarryIt(string header)
        {
            using var upstreamRequest = new HttpRequestMessage(HttpMethod.Post, "https://example.com/")
            {
                Content = new StringContent("body")
            };
            var upstreamRequestCanCarryIt = upstreamRequest.Headers.TryAddWithoutValidation(header, "probe-value");

            config.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                RequestHeaders = new List<string> { header }
            });

            var result = subject.Validate(null, config);

            Assert.That(result.Succeeded, Is.EqualTo(upstreamRequestCanCarryIt));
        }

        [Test]
        public void ItSucceedsWhenAnApiHasNullHeaderCollections()
        {
            config.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                RequestHeaders = null,
                ResponseHeaders = null
            });

            var result = subject.Validate(null, config);

            Assert.That(result.Succeeded, Is.True);
        }
    }
}
