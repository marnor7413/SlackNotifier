using MailService.Infrastructure.Factories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SN.Application.Interfaces;
using SN.Application.Services;
using SN.ConsoleApp.Constants;
using SN.Infrastructure.Services.Gmail;
using SN.Infrastructure.Services.Slack;

namespace SN.ConsoleApp.Extensions;

public static class ServiceCollectionExtensions
{
    private const string GmailApiBaseUrlKey = ApplicationConstants.Appsettings.GmailApiBaseUrlKey;
    private const string SlackApiBaseUrlKey = ApplicationConstants.Appsettings.SlackApiBaseUrlKey;

    public static void AddHttpClients(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddTransient<GmailApiService>();
        serviceCollection.AddHttpClient<GmailApiService>((httpClient) =>
        {
            var baseUri = configuration.GetSection(GmailApiBaseUrlKey).Value;
            httpClient.BaseAddress = new Uri(baseUri);
        });

        serviceCollection.AddTransient<SlackApiService>();
        serviceCollection.AddHttpClient<SlackApiService>((httpClient) =>
        {
            var baseUri = configuration.GetSection(SlackApiBaseUrlKey).Value;
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.BaseAddress = new Uri(baseUri);
        });
    }

    public static void AddServices(this  IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IGmailInboxService, GmailOauth2Service>();
        serviceCollection.AddScoped<IGmailInboxService, GmailApplicationPasswordService>();
        serviceCollection.AddScoped<IMessageForwarderService, MessageForwarderService>();
        serviceCollection.AddScoped<IGmailApiService, GmailApiService>();
        serviceCollection.AddScoped<IGmailPayloadService, GmailPayloadService>();
        serviceCollection.AddScoped<IGoogleAuthService, GoogleAuthService>();
        serviceCollection.AddScoped<IIOService, IOService>();
        serviceCollection.AddScoped<IGmailServiceFactory, GmailServiceFactory>();
        serviceCollection.AddScoped<IMessageTypeService, MessageTypeService>();
        serviceCollection.AddTransient<ISlackService, SlackService>();
        serviceCollection.AddTransient<ISlackApiService, SlackApiService>();
        serviceCollection.AddTransient<IGmailImapService, GmailImapService>();
    }
}
