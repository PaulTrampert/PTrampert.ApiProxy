using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace PTrampert.ApiProxy.Test
{
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void ItBindsAValidConfiguration()
        {
            var services = new ServiceCollection();
            services.AddApiProxy(cfg => cfg.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                RequestHeaders = new List<string> { "X-Some-Header" }
            }));
            var provider = services.BuildServiceProvider();

            var config = provider.GetRequiredService<IOptions<ApiProxyConfig>>().Value;

            Assert.That(config["fake"].BaseUrl, Is.EqualTo("https://example.com"));
        }

        [Test]
        public void ItThrowsWhenAConfiguredHeaderIsReserved()
        {
            var services = new ServiceCollection();
            services.AddApiProxy(cfg => cfg.Add("fake", new ApiConfig
            {
                BaseUrl = "https://example.com",
                RequestHeaders = new List<string> { "Content-Type" }
            }));
            var provider = services.BuildServiceProvider();

            // The delegate is cast explicitly because the Action and TestDelegate overloads of Throws are
            // otherwise ambiguous when building for net8.0.
            var exception = Assert.Throws<OptionsValidationException>((Action)(() => _ = provider.GetRequiredService<IOptions<ApiProxyConfig>>().Value));

            Assert.That(exception?.Message, Does.Contain("Content-Type"));
        }

        [Test]
        public void ItFailsHostStartupWhenAConfiguredHeaderIsReserved()
        {
            // The delegate is cast explicitly because the Func<Task> and AsyncTestDelegate overloads
            // of ThrowsAsync are otherwise ambiguous. Build and start are both inside it because the
            // startup validator may run in either step depending on the target framework.
            var exception = Assert.ThrowsAsync<OptionsValidationException>((Func<Task>)(async () =>
            {
                using var host = new HostBuilder()
                    .ConfigureServices(services => services.AddApiProxy(cfg => cfg.Add("fake", new ApiConfig
                    {
                        BaseUrl = "https://example.com",
                        ResponseHeaders = new List<string> { "Content-Type" }
                    })))
                    .Build();
                await host.StartAsync();
            }));

            Assert.That(exception?.Message, Does.Contain("Content-Type"));
        }
    }
}
