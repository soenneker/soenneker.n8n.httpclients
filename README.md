[![](https://img.shields.io/nuget/v/soenneker.n8n.httpclients.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.n8n.httpclients/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.n8n.httpclients/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.n8n.httpclients/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.n8n.httpclients.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.n8n.httpclients/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.n8n.httpclients/codeql.yml?style=for-the-badge&label=codeql)](https://github.com/soenneker/soenneker.n8n.httpclients/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.N8n.HttpClients

Provides cached, API-key-authenticated `HttpClient` instances for one or more n8n servers.

## Installation

```bash
dotnet add package Soenneker.N8n.HttpClients
```

## Configuration

```json
{
  "N8n": {
    "ApiKey": "your-api-key",
    "ClientBaseUrl": "https://n8n.example.com/api/v1"
  }
}
```

## Usage

```csharp
using Soenneker.N8n.HttpClients.Abstract;
using Soenneker.N8n.HttpClients.Registrars;

services.AddN8nOpenApiHttpClientAsSingleton();

IN8nOpenApiHttpClient n8n = serviceProvider
    .GetRequiredService<IN8nOpenApiHttpClient>();

HttpClient client = await n8n.Get(cancellationToken);
```

Use `Get(apiKey, baseUrl)` to connect to another n8n server. Equivalent connection settings reuse the same client within the provider's lifetime.

The default authentication header is `X-N8N-API-KEY: {token}`. `N8n:AuthHeaderName` and `N8n:AuthHeaderValueTemplate` can override it for a compatible gateway.

Do not dispose a returned `HttpClient`; the registered provider owns it and removes it from the cache when disposed.
