using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SN.Application.Builders;
using SN.Application.Interfaces;
using SN.Application.Services;
using SN.ConsoleApp.Constants;
using SN.ConsoleApp.Diagnostics;
using SN.ConsoleApp.Extensions;
using SN.ConsoleApp.Services;

var builder = Host.CreateApplicationBuilder(args);
FirstChanceExceptionLogger.EnableIfConfigured(builder.Configuration);
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(opt =>
{
    opt.SingleLine = false;
    opt.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
});

builder.Services.ConfigureOpenTelemetry();

builder.Services.AddHttpClients(builder.Configuration);
builder.Services.ConfigureOptionsFromAppsettings(builder.Configuration);
builder.Services.AddServices();
builder.Services.AddScoped<ISlackBlockBuilder, SlackBlockBuilder>();
builder.Services.AddTransient<IImapConnectionClient, ImapConnectionClient>();
builder.Services.AddHostedService<MessageForwarderHostedService>();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("---> Environment set to {Environment}", builder.Environment.EnvironmentName);
logger.LogInformation("---> Starting version {Version} of application.", ApplicationConstants.Version);
logger.LogInformation("---> Application started. Waiting for shutdown signal (SIGTERM/CTRL+C).");

await host.RunAsync();
