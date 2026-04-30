using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SN.Application.Builders;
using SN.Application.Interfaces;
using SN.ConsoleApp.Extensions;
using SN.ConsoleApp.Services;

namespace SN.ConsoleApp;

class Program
{
    private static readonly string version = "1.0.2";

    static async Task Main(string[] args)
    {
        var environment = Environment
            .GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
            ? "Development"
            : "Production";
        Console.WriteLine($"[{DateTime.Now.ToLocalTime()}] Environment set to {environment}");
        Console.WriteLine($"[{DateTime.Now.ToLocalTime()}] Starting version {version} of application.");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHttpClients(configuration);
                services.ConfigureOptionsFromAppsettings(configuration);
                services.AddSingleton<IConfiguration>(configuration);
                services.AddServices();
                services.AddScoped<ISlackBlockBuilder, SlackBlockBuilder>();
                services.AddHostedService<MessageForwarderHostedService>();
            })
            .Build();

        Console.WriteLine("Console application started. Press Enter to stop.");

        var runTask = host.RunAsync();
        await Task.Run(() => Console.ReadLine());
        await host.StopAsync();
        await runTask;
    }
}
