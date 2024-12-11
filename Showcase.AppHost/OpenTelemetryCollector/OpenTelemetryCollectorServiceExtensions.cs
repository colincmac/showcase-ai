using Aspire.Hosting.Lifecycle;
using Showcase.AppHost.OpenTelemetryCollector;

namespace Showcase.AppHost.OpenTelemetryCollector;

internal static class OpenTelemetryCollectorServiceExtensions
{
    public static IDistributedApplicationBuilder AddOpenTelemetryCollectorInfrastructure(this IDistributedApplicationBuilder builder)
    {
        builder.Services.TryAddLifecycleHook<OpenTelemetryCollectorLifecycleHook>();

        return builder;
    }
}
