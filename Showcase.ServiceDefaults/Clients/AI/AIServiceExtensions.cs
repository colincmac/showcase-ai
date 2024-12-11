using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;

namespace Showcase.ServiceDefaults.Clients.AI;
public static class AIServiceExtensions
{

    public static void AddAIServices(this IHostApplicationBuilder hostBuilder, string serviceName, string? chatDeploymentName = null, string? embeddingDeploymentName = null)
    {
        var sourceName = Guid.NewGuid().ToString();
        var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .Build();

        // Configure caching
        IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        hostBuilder.AddAzureOpenAIClient(serviceName);
    }

}
