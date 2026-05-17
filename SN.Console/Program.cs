using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SN.Application.Builders;
using SN.Application.Interfaces;
using SN.Application.Services;
using SN.ConsoleApp.Extensions;
using SN.ConsoleApp.Services;

namespace SN.ConsoleApp;

class Program
{
    private static readonly string version = "1.3.2";
    private const string DevelopEnvironment = "Development";
    private const string ProductionEnvironment = "Production";

    static async Task Main(string[] args)
    {
        var environment = Environment
            .GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == DevelopEnvironment
                ? DevelopEnvironment
                : ProductionEnvironment;

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);

        if (environment == DevelopEnvironment)
        {
            builder.AddUserSecrets<Program>();
        }

        var configuration = builder
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logger => 
            {
                logger.ClearProviders();
                logger.AddSimpleConsole(opt =>
                {
                    opt.SingleLine = false;
                    opt.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                });
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHttpClients(configuration);
                services.ConfigureOptionsFromAppsettings(configuration);
                services.AddSingleton<IConfiguration>(configuration);
                services.AddServices();
                services.AddScoped<ISlackBlockBuilder, SlackBlockBuilder>();
                services.AddTransient<IImapConnectionClient, ImapConnectionClient>();
                services.AddHostedService<MessageForwarderHostedService>();
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("---> Environment set to {Environment}", environment);
        logger.LogInformation("---> Starting version {Version} of application.", version);
        logger.LogInformation("---> Console application started. Press Enter to stop.");

        var runTask = host.RunAsync();
        await Task.Run(() => Console.ReadLine());
        await host.StopAsync();
        await runTask;
    }
}
