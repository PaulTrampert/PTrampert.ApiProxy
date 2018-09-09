using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using PTrampert.ApiProxy.Authentication;

namespace PTrampert.ApiProxy.Test.Authentication
{
    public class BasicAuthenticationTests
    {
        private BasicAuthentication subject;

        [SetUp]
        public void SetUp()
        {
            subject = new BasicAuthentication("id", "secret");
        }

        [Test]
        public async Task ItGeneratesBasicAuthHeader()
        {
            var result = await subject.GetAuthenticationHeader();
            Assert.That(result.Scheme, Is.EqualTo("Basic"));
        }

        [Test]
        public async Task ItGeneratesTheCorrectAuthString()
        {
            var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes("id:secret"));

            var result = await subject.GetAuthenticationHeader();

            Assert.That(result.Parameter, Is.EqualTo(expected));
        }
    }
}
