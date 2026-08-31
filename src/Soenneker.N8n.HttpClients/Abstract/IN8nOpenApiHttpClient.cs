using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace Soenneker.N8n.HttpClients.Abstract;

/// <summary>
/// Provides cached, authenticated HTTP clients for one or more n8n servers.
/// </summary>
public interface IN8nOpenApiHttpClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets a client using the configured API key and base URL.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configured client.</returns>
    ValueTask<HttpClient> Get(CancellationToken cancellationToken = default);

    /// <summary>Gets a client for a specific API key using the configured base URL.</summary>
    ValueTask<HttpClient> Get(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>Gets a client for a specific n8n connection.</summary>
    ValueTask<HttpClient> Get(string apiKey, string baseUrl, CancellationToken cancellationToken = default);
}
