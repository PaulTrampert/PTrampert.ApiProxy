using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace PTrampert.ApiProxy.Test
{
    class TestHttpHandler : HttpMessageHandler
    {
        public string LastRequestUrl { get; private set; }

        public string LastRequestBody { get; private set; }

        public string LastRequestMediaType { get; private set; }

        public AuthenticationHeaderValue LastRequestAuthenticationHeader { get; private set; }

        public HttpResponseMessage NextResponse { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUrl = request.RequestUri.ToString();
            LastRequestAuthenticationHeader = request.Headers.Authorization;
            if (request.Content != null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync();
                LastRequestMediaType = request.Content?.Headers.ContentType.MediaType;
            }

            return NextResponse;
        }
    }
}
