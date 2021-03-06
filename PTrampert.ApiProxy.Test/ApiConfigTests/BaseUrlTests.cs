﻿using NUnit.Framework;

namespace PTrampert.ApiProxy.Test.ApiConfigTests
{
    public class BaseUrlTests
    {
        private ApiConfig subject;

        [SetUp]
        public void SetUp()
        {
            subject = new ApiConfig
            {
                BaseUrl = "https://example.com/some/route/"
            };
        }

        [Test]
        public void BaseUrlHasTrailingSlashTrimmed()
        {
            Assert.That(subject.BaseUrl, Is.EqualTo("https://example.com/some/route"));
        }
    }
}
