using Soenneker.N8n.HttpClients.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.N8n.HttpClients.Tests;

[Collection("Collection")]
public sealed class N8nOpenApiHttpClientTests : FixturedUnitTest
{
    private readonly IN8nOpenApiHttpClient _httpclient;

    public N8nOpenApiHttpClientTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _httpclient = Resolve<IN8nOpenApiHttpClient>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
