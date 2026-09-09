using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soenneker.N8n.HttpClients.Abstract;
using Soenneker.N8n.HttpClients.Registrars;

namespace Soenneker.N8n.HttpClients.Tests;

public sealed class N8nNetworkPolicyTests
{
    [Test]
    [Arguments(null)]
    [Arguments("false")]
    public async Task Default_and_explicit_public_only_block_loopback(string? setting)
    {
        await using ServiceProvider services = CreateServices(setting);
        IN8nOpenApiHttpClient provider = services.GetRequiredService<IN8nOpenApiHttpClient>();
        HttpClient client = await provider.Get("test-secret", "http://127.0.0.1:1");
        Func<Task> send = () => client.GetAsync("/api/v1/workflows");
        await send.Should().ThrowAsync<HttpRequestException>().WithMessage("*blocked*");
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Private_access_connects_locally_but_never_follows_redirects(bool redirect)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        using var destination = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        destination.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        int destinationPort = ((IPEndPoint)destination.LocalEndpoint).Port;
        await using ServiceProvider services = CreateServices("true");
        IN8nOpenApiHttpClient provider = services.GetRequiredService<IN8nOpenApiHttpClient>();
        HttpClient client = await provider.Get("test-secret", $"http://127.0.0.1:{port}");
        Task server = Respond();
        using HttpResponseMessage response = await client.GetAsync("/api/v1/workflows", timeout.Token);
        response.StatusCode.Should().Be(redirect ? HttpStatusCode.Redirect : HttpStatusCode.OK);
        await server;
        destination.Pending().Should().BeFalse();

        async Task Respond()
        {
            using TcpClient connection = await listener.AcceptTcpClientAsync(timeout.Token);
            await using NetworkStream stream = connection.GetStream();
            using var reader = new StreamReader(stream, leaveOpen: true);
            var request = new StringBuilder();
            string? line;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(timeout.Token))) request.AppendLine(line);
            request.ToString().Should().Contain("X-N8N-API-KEY: test-secret");
            string status = redirect ? "302 Found" : "200 OK";
            string location = redirect ? $"Location: http://127.0.0.1:{destinationPort}/private\r\n" : "";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(
                $"HTTP/1.1 {status}\r\n{location}Content-Length: 0\r\nConnection: close\r\n\r\n"), timeout.Token);
        }
    }

    [Test]
    public void Invalid_policy_is_rejected()
    {
        using ServiceProvider services = CreateServices("typo");
        Action resolve = () => services.GetRequiredService<IN8nOpenApiHttpClient>();
        resolve.Should().Throw<InvalidOperationException>();
    }

    private static ServiceProvider CreateServices(string? setting)
    {
        var services = new ServiceCollection();
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["N8n:AllowPrivateNetworkAccess"] = setting }).Build();
        services.AddSingleton(config);
        services.AddLogging();
        services.AddN8nOpenApiHttpClientAsSingleton();
        return services.BuildServiceProvider();
    }
}
