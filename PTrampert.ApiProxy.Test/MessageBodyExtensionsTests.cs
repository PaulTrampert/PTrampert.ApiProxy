using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using NUnit.Framework;

namespace PTrampert.ApiProxy.Test;

public class MessageBodyExtensionsTests
{
    private DefaultHttpContext context;

    [SetUp]
    public void SetUp()
    {
        context = new DefaultHttpContext();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ItLetsTheServerDecideWhetherARequestHasABody(bool canHaveBody)
    {
        context.Features.Set<IHttpRequestBodyDetectionFeature>(new BodyDetectionFeature(canHaveBody));
        // The framing headers would say the opposite, so this only passes if the server's answer wins.
        if (canHaveBody)
        {
            context.Request.ContentLength = 0;
        }
        else
        {
            context.Request.ContentLength = 5;
        }

        Assert.That(context.Request.HasBody(), Is.EqualTo(canHaveBody));
    }

    [Test]
    public void ItTreatsARequestWithAContentLengthAsHavingABodyWhenTheServerDoesNotSay()
    {
        context.Request.ContentLength = 5;

        Assert.That(context.Request.HasBody(), Is.True);
    }

    [Test]
    public void ItTreatsAChunkedRequestAsHavingABodyWhenTheServerDoesNotSay()
    {
        context.Request.Headers.TransferEncoding = "chunked";

        Assert.That(context.Request.HasBody(), Is.True);
    }

    [TestCase(null)]
    [TestCase(0L)]
    public void ItTreatsARequestWithoutFramingAsBodylessWhenTheServerDoesNotSay(long? contentLength)
    {
        context.Request.ContentLength = contentLength;

        Assert.That(context.Request.HasBody(), Is.False);
    }

    [Test]
    public void ItTreatsAResponseWithoutAContentLengthAsHavingABody()
    {
        // A pipe's stream cannot seek, so the content cannot compute a length, as with a chunked or HTTP/2 response.
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new Pipe().Reader.AsStream())
        };

        Assert.That(response.Content.Headers.ContentLength, Is.Null);
        Assert.That(response.HasBody("GET"), Is.True);
    }

    [TestCase("GET", HttpStatusCode.NoContent)]
    [TestCase("GET", HttpStatusCode.NotModified)]
    [TestCase("HEAD", HttpStatusCode.OK)]
    [TestCase("head", HttpStatusCode.OK)]
    public void ItTreatsAResponseThatCannotHaveABodyAsBodyless(string method, HttpStatusCode status)
    {
        using var response = new HttpResponseMessage(status)
        {
            Content = new ByteArrayContent(new byte[] { 1, 2, 3 })
        };

        Assert.That(response.HasBody(method), Is.False);
    }

    [Test]
    public void ItTreatsAResponseWithAZeroContentLengthAsBodyless()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[0])
        };

        Assert.That(response.HasBody("GET"), Is.False);
    }

    private class BodyDetectionFeature : IHttpRequestBodyDetectionFeature
    {
        public BodyDetectionFeature(bool canHaveBody)
        {
            CanHaveBody = canHaveBody;
        }

        public bool CanHaveBody { get; }
    }
}
