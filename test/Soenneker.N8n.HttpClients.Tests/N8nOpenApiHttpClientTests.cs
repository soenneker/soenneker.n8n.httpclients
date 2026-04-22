using Soenneker.N8n.HttpClients.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.N8n.HttpClients.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class N8nOpenApiHttpClientTests : HostedUnitTest
{
    private readonly IN8nOpenApiHttpClient _httpclient;

    public N8nOpenApiHttpClientTests(Host host) : base(host)
    {
        _httpclient = Resolve<IN8nOpenApiHttpClient>(true);
    }

    [Test]
    public void Default()
    {

    }
}
