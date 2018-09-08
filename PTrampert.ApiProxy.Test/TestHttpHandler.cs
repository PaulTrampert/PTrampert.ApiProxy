using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PTrampert.ApiProxy.Test
{
    class TestHttpHandler : HttpMessageHandler
    {
        public HttpRequestMessage LastRequest { get; private set; }

        public HttpResponseMessage NextResponse { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(NextResponse);
        }
    }
}
