using MailService.Application.Interfaces;
using MailService.Application.Services;
using MailService.ConsoleApp.Configuration;
using MailService.ConsoleApp.Extensions;
using MailService.Infrastructure.EmailServices;
using MailService.Infrastructure.Factories;
using MailService.Infrastructure.SlackServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SN.Infrastructure.Services.Gmail;
using SN.Infrastructure.Services.Slack;

namespace MailService;

class Program
{
    private const string GmailBaseUriKey = "Appsettings:GmailBaseUri";
    private const string SlackBaseUriKey = "Appsettings:SlackBaseUri";
    private static IMessageForwarderService _messageForwarder;
    private static Timer _timer;
    private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Allow only one execution at a time
    private static IConfiguration configuration;

    static async Task Main(string[] args)
    {

        configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        
        using var host = CreateHostBuilder(args).Build();

        _messageForwarder = host.Services.GetRequiredService<IMessageForwarderService>();
        await _messageForwarder.Run();
        //_timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
        
        //Console.WriteLine("Console application started.");
        //while (true)
        //{
        //    Console.WriteLine("Press Enter to exit");
        //    var input = Console.ReadLine();
            
        //    if (string.IsNullOrEmpty(input))
        //    {
        //        host.StopAsync()
        //            .Wait();
        //        return;
        //    }
        //    Console.Clear();
        //}
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                List<SecretsOptions> secrets = configuration.GetJsonSecrets("secrets.json");
                services.AddSingleton(secrets);
                services.AddSingleton(provider =>
                    Options.Create(provider.GetRequiredService<List<SecretsOptions>>()));
                
                services.AddHttpClient<GmailApiService>((httpClient) =>
                {
                    var baseUri = configuration.GetSection(GmailBaseUriKey).Value;
                    httpClient.BaseAddress = new Uri(baseUri);
                });

                services.AddHttpClient<SlackService>((httpClient) =>
                {
                    var baseUri = configuration.GetSection(SlackBaseUriKey).Value;
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                    httpClient.BaseAddress = new Uri(baseUri);
                });

                services.AddScoped<IMessageForwarderService, MessageForwarderService>();
                services.AddScoped<IGmailApiService, GmailApiService>();
                services.AddScoped<IGmailServiceFactoryOauth, GmailServiceFactoryOauth>();
                services.AddScoped<ISlackService, SlackService>();
            });

    private static void DoWork(object state)
    {
        // Ensure only one execution is active at a time
        if (_semaphore.Wait(0))
        {
            try
            {
                // Call the FetchEmails method on the emailService
                _messageForwarder.Run();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
