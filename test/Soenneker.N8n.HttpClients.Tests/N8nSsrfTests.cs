using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Soenneker.Utils.HttpClientCache.Abstract;
using Soenneker.Utils.HttpClientCache.Ssrf;
using Soenneker.Validators.IpAddresses.Ssrf;

namespace Soenneker.N8n.HttpClients.Tests;

public sealed class N8nSsrfTests
{
    [Test]
    public async Task Public_to_private_redirect_does_not_forward_credentials()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var inner = DispatchProxy.Create<IHttpClientCache, ControlledTransportCache>();
        var transport = (ControlledTransportCache)inner;
        transport.PublicPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var cache = new SsrfHttpClientCache(inner, new SsrfIpAddressValidator(NullLogger<SsrfIpAddressValidator>.Instance));
        using var provider = new N8nOpenApiHttpClient(inner, cache, new ConfigurationBuilder().Build());
        HttpClient client = await provider.Get("test-secret", "http://public.example");
        Task server = Respond();
        using HttpResponseMessage response = await client.GetAsync("/api/v1/workflows", timeout.Token);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        transport.Connections.Should().Be(1);
        await server;

        async Task Respond()
        {
            using TcpClient connection = await listener.AcceptTcpClientAsync(timeout.Token);
            await using NetworkStream stream = connection.GetStream();
            using var reader = new StreamReader(stream, leaveOpen: true);
            var request = new StringBuilder();
            string? line;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(timeout.Token))) request.AppendLine(line);
            request.ToString().Should().Contain("X-N8N-API-KEY: test-secret");
            byte[] responseBytes = Encoding.ASCII.GetBytes("HTTP/1.1 302 Found\r\nLocation: https://127.0.0.1/private\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(responseBytes, timeout.Token);
        }
    }

    [Test]
    public async Task Public_preflight_does_not_authorize_private_connection_time_dns()
    {
        var validator = new SsrfIpAddressValidator(NullLogger<SsrfIpAddressValidator>.Instance);
        validator.Validate(IPAddress.Parse("8.8.8.8")).Should().BeTrue();
        var inner = DispatchProxy.Create<IHttpClientCache, ControlledTransportCache>();
        var transport = (ControlledTransportCache)inner;
        transport.Rebind = true;
        transport.PublicPort = 443;
        using var cache = new SsrfHttpClientCache(inner, validator);
        using var provider = new N8nOpenApiHttpClient(inner, cache, new ConfigurationBuilder().Build());
        HttpClient client = await provider.Get("test-secret", "https://public.example");
        Func<Task> send = () => client.GetAsync("/api/v1/workflows");
        await send.Should().ThrowAsync<HttpRequestException>().WithMessage("*blocked*");
        transport.Connections.Should().Be(1);
    }
}
