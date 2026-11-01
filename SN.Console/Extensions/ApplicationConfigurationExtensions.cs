using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using SN.Application.Metrics;
using SN.Application.Options;
using SN.ConsoleApp.Constants;

namespace SN.ConsoleApp.Extensions;

public static class ApplicationConfigurationExtensions
{
    public static void ConfigureOpenTelemetry(this IServiceCollection services)
    {
        var applicationVersion = new KeyValuePair<string, object>("service.version", ApplicationConstants.Version);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddAttributes([applicationVersion]))
            .WithMetrics(metrics => metrics
                .AddMeter("System.Runtime")
                .AddMeter("System.Net.Http")
                .AddMeter(SlackNotifierMetrics.MeterName)
                // dotnet.exceptions is dropped from the exported metrics. It counts every
                // thrown exception, including those handled inside libraries, so it cannot
                // separate expected exceptions from real errors. Real errors are counted in
                // slacknotifier.errors and slacknotifier.runs.failed. Use
                // FirstChanceExceptionLogger to investigate exceptions.
                .AddView(instrumentName: "dotnet.exceptions", MetricStreamConfiguration.Drop))
            .UseOtlpExporter();
    }

    public static void ConfigureOptionsFromAppsettings(
      this IServiceCollection serviceCollection,
      IConfiguration configuration)
    {
        serviceCollection.Configure<GmailImapSecretsOptions>(
            configuration.GetSection(ApplicationConstants.Appsettings.GmailImapSecrets));

        serviceCollection.Configure<SlackSecretsOptions>(
            configuration.GetSection(ApplicationConstants.Appsettings.SlackSecrets));
    }
}
