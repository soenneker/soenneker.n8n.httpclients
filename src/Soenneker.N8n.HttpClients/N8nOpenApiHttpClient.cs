using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Soenneker.Dtos.HttpClientOptions;
using Soenneker.Extensions.Configuration;
using Soenneker.Hashing.XxHash;
using Soenneker.N8n.HttpClients.Abstract;
using Soenneker.Utils.HttpClientCache.Abstract;

namespace Soenneker.N8n.HttpClients;

public sealed class N8nOpenApiHttpClient : IN8nOpenApiHttpClient
{
    private readonly IHttpClientCache _httpClientCache;
    private readonly IConfiguration _configuration;
    private readonly string? _baseUrl;
    private readonly string _authHeaderName;
    private readonly string _authHeaderValueTemplate;
    private readonly ConcurrentDictionary<string, byte> _clientIds = new();
    private readonly string _providerId = Guid.NewGuid().ToString("N");

    public N8nOpenApiHttpClient(IHttpClientCache httpClientCache, IConfiguration config)
    {
        _httpClientCache = httpClientCache;
        _configuration = config;
        _baseUrl = config["N8n:ClientBaseUrl"];
        _authHeaderName = config["N8n:AuthHeaderName"] ?? "X-N8N-API-KEY";
        _authHeaderValueTemplate = config["N8n:AuthHeaderValueTemplate"] ?? "{token}";
    }

    public ValueTask<HttpClient> Get(CancellationToken cancellationToken = default)
    {
        return Get(_configuration.GetValueStrict<string>("N8n:ApiKey"), GetConfiguredBaseUrl(), cancellationToken);
    }

    public ValueTask<HttpClient> Get(string apiKey, CancellationToken cancellationToken = default)
    {
        return Get(apiKey, GetConfiguredBaseUrl(), cancellationToken);
    }

    public ValueTask<HttpClient> Get(string apiKey, string baseUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        var baseUri = new Uri(baseUrl, UriKind.Absolute);
        string clientId = GetClientId(apiKey, baseUri);
        _clientIds.TryAdd(clientId, 0);

        return _httpClientCache.Get(clientId,
            (apiKey, baseUri, authHeaderName: _authHeaderName, authHeaderValueTemplate: _authHeaderValueTemplate), static state =>
            {
                string authHeaderValue = state.authHeaderValueTemplate.Replace("{token}", state.apiKey, StringComparison.Ordinal);

                return new HttpClientOptions
                {
                    BaseAddress = state.baseUri,
                    DefaultRequestHeaders = new Dictionary<string, string>
                    {
                        {state.authHeaderName, authHeaderValue},
                    }
                };
            }, cancellationToken);
    }

    private string GetClientId(string apiKey, Uri baseUri)
    {
        string value = string.Concat(_providerId, "\0", apiKey, "\0", baseUri, "\0", _authHeaderName, "\0", _authHeaderValueTemplate);

        return $"{nameof(N8nOpenApiHttpClient)}:{XxHash3Util.Hash(value)}";
    }

    private string GetConfiguredBaseUrl()
    {
        return _baseUrl ?? throw new InvalidOperationException("N8n:ClientBaseUrl must be configured when a base URL is not supplied explicitly.");
    }

    public void Dispose()
    {
        foreach (string clientId in _clientIds.Keys)
        {
            _httpClientCache.RemoveSync(clientId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (string clientId in _clientIds.Keys)
        {
            await _httpClientCache.Remove(clientId);
        }
    }
}
