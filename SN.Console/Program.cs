using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SN.Application.Interfaces;
using SN.ConsoleApp.Extensions;

namespace MailService;

class Program
{
    private static IMessageForwarderService _messageForwarder;
    private static Timer _timer;
    private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Allow only one execution at a time

    static async Task Main(string[] args)
    {
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var environmentName = GetEnvironment ?? "Development";
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args)
                    .Build();

                services.AddHttpClients(configuration);
                services.ConfigureOptionsFromAppsettings(configuration);
                services.AddSingleton<IConfiguration>(configuration);
                services.AddServices();
            })
            .Build();


        _messageForwarder = host.Services.GetRequiredService<IMessageForwarderService>();
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));

        Console.WriteLine("Console application started.");
        while (true)
        {
            Console.WriteLine("Press Enter to exit");
            var input = Console.ReadLine();

            if (string.IsNullOrEmpty(input))
            {
                host.StopAsync()
                    .Wait();
                return;
            }
            Console.Clear();
        }
    }

    public static string GetEnvironment
    {
        get
        {
            string environmentName = null;

            #if DEBUG
                environmentName = "Development";
            #elif RELEASE
                environmentName = "Production";
            #endif

            return environmentName;
        }
    }

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
