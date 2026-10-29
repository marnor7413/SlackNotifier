using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SN.Application.Builders;
using SN.Application.Interfaces;
using SN.Application.Services;
using SN.ConsoleApp.Constants;
using SN.ConsoleApp.Extensions;
using SN.ConsoleApp.Services;

var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    == ApplicationConstants.Environments.Development;
var environment = isDevelopment
    ? ApplicationConstants.Environments.Development
    : ApplicationConstants.Environments.Production;

Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);
var builder = Host.CreateApplicationBuilder(args);

if (isDevelopment)
{
    builder.Configuration.AddUserSecrets<App>();
}

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(opt =>
{
    opt.SingleLine = false;
    opt.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
});

builder.Services.AddHttpClients(builder.Configuration);
builder.Services.ConfigureOptionsFromAppsettings(builder.Configuration);
builder.Services.AddServices();
builder.Services.AddScoped<ISlackBlockBuilder, SlackBlockBuilder>();
builder.Services.AddTransient<IImapConnectionClient, ImapConnectionClient>();
builder.Services.AddHostedService<MessageForwarderHostedService>();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<App>>();
logger.LogInformation("---> Environment set to {Environment}", environment);
logger.LogInformation("---> Starting version {Version} of application.", ApplicationConstants.Version);
logger.LogInformation("---> Application started. Waiting for shutdown signal (SIGTERM/CTRL+C).");

await host.RunAsync();
