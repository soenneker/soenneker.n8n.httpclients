using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.N8n.HttpClients.Abstract;
using Soenneker.Utils.HttpClientCache.Ssrf.Registrars;

namespace Soenneker.N8n.HttpClients.Registrars;

/// <summary>
/// Registers the OpenAPI HttpClient wrapper for dependency injection.
/// </summary>
public static class N8nOpenApiHttpClientRegistrar
{
    /// <summary>
    /// Adds <see cref="N8nOpenApiHttpClient"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddN8nOpenApiHttpClientAsSingleton(this IServiceCollection services)
    {
        services.AddSsrfHttpClientCacheAsSingleton()
                .TryAddSingleton<IN8nOpenApiHttpClient, N8nOpenApiHttpClient>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="N8nOpenApiHttpClient"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddN8nOpenApiHttpClientAsScoped(this IServiceCollection services)
    {
        services.AddSsrfHttpClientCacheAsSingleton()
                .TryAddScoped<IN8nOpenApiHttpClient, N8nOpenApiHttpClient>();

        return services;
    }
}
