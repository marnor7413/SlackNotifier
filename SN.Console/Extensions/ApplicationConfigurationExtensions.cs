using SN.ConsoleApp.Constants;

namespace SN.ConsoleApp.Extensions;

public static class ApplicationConfigurationExtensions
{
    public static void ConfigureOpenTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource 
                => resource.AddAttributes([ new KeyValuePair<string, object>("service.version", ApplicationConstants.Version) ]))
            .WithMetrics(metrics => metrics
                .AddMeter("System.Runtime")
                .AddMeter("System.Net.Http"))
            .UseOtlpExporter();
    }
}
