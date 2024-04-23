using MailService.ConsoleApp.Extensions;
using MailService.Infrastructure.Factories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SN.Application.Interfaces;
using SN.Application.Options;
using SN.Application.Services;
using SN.Infrastructure.Services.Gmail;
using SN.Infrastructure.Services.Slack;

namespace SN.ConsoleApp.Extensions;

public static class ServiceCollectionExtensions
{
    private const string GmailBaseUriKey = "Appsettings:GmailBaseUri";
    private const string SlackBaseUriKey = "Appsettings:SlackBaseUri";
    private const string SlackCredentialsKey = "Appsettings:SlackCredentialsFilename";

    public static void AddHttpClients(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddHttpClient<GmailApiService>((httpClient) =>
        {
            var baseUri = configuration.GetSection(GmailBaseUriKey).Value;
            httpClient.BaseAddress = new Uri(baseUri);
        });

        serviceCollection.AddHttpClient<SlackApiService>((httpClient) =>
        {
            var baseUri = configuration.GetSection(SlackBaseUriKey).Value;
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.BaseAddress = new Uri(baseUri);
        });
    }

    public static void ConfigureOptionsFromAppsettings(this  IServiceCollection serviceCollection, IConfiguration configuration)
    {
        List<SecretsOptions> secrets = configuration.GetJsonSecrets(configuration.GetSection(SlackCredentialsKey).Value);
        serviceCollection.AddSingleton(secrets);
        serviceCollection.AddSingleton(provider => Options.Create(provider.GetRequiredService<List<SecretsOptions>>()));
    }

    public static void AddServices(this  IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IGmailInboxService, GmailInboxService>();
        serviceCollection.AddScoped<IMessageForwarderService, MessageForwarderService>();
        serviceCollection.AddScoped<IGmailApiService, GmailApiService>();
        serviceCollection.AddScoped<IGmailPayloadService, GmailPayloadService>();
        serviceCollection.AddScoped<IGoogleAuthService, GoogleAuthService>();
        serviceCollection.AddScoped<IIOService, IOService>();
        serviceCollection.AddScoped<IGmailServiceFactory, GmailServiceFactory>();
        serviceCollection.AddScoped<IMessageTypeService, MessageTypeService>();
        serviceCollection.AddScoped<ISlackService, SlackService>();
        serviceCollection.AddScoped<ISlackApiService, SlackApiService>();
    }

}
