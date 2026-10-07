using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// The Content-Type of the last request exactly as it was set, without parsing, so that tests can assert
        /// values the proxy cannot parse are still forwarded unchanged.
        /// </summary>
        public string LastRequestRawContentType { get; private set; }

        public AuthenticationHeaderValue LastRequestAuthenticationHeader { get; private set; }

        /// <summary>
        /// The headers of the last request, copied out of the request because the request is disposed once it has been sent.
        /// Deliberately keyed case sensitively, so that tests can assert header names are proxied exactly as they were given.
        /// </summary>
        public IDictionary<string, string[]> LastRequestHeaders { get; private set; } = new Dictionary<string, string[]>(StringComparer.Ordinal);

        /// <summary>
        /// The content headers of the last request, keyed case sensitively like <see cref="LastRequestHeaders"/>.
        /// Empty when the request had no content.
        /// </summary>
        public IDictionary<string, string[]> LastRequestContentHeaders { get; private set; } = new Dictionary<string, string[]>(StringComparer.Ordinal);

        public HttpResponseMessage NextResponse { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUrl = request.RequestUri.ToString();
            LastRequestAuthenticationHeader = request.Headers.Authorization;
            LastRequestHeaders = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray(), StringComparer.Ordinal);
            LastRequestContentHeaders = request.Content?.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray(), StringComparer.Ordinal)
                ?? new Dictionary<string, string[]>(StringComparer.Ordinal);
            if (request.Content != null)
            {
                // Read before the body: ReadAsStringAsync parses the Content-Type to find a charset, which
                // normalises the stored value.
                LastRequestRawContentType = request.Content.Headers.NonValidated.TryGetValues("Content-Type", out var contentType)
                    ? contentType.ToString()
                    : null;
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
                LastRequestMediaType = request.Content.Headers.ContentType?.MediaType;
            }

            return NextResponse;
        }
    }
}
