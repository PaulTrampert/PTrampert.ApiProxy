using System.Collections.Generic;
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
