using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace PTrampert.ApiProxy.Test
{
    public class ApiProxyControllerTests
    {
        private ApiProxyController subject;

        [SetUp]
        public void SetUp()
        {
            subject = new ApiProxyController();
        }

        [Test]
        public void Pass()
        {
            Assert.True(true);
        }
    }
}
