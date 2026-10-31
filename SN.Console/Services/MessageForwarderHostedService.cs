using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SN.Application.Interfaces;

namespace SN.ConsoleApp.Services;

public class MessageForwarderHostedService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<MessageForwarderHostedService> logger;
    private readonly TimeSpan interval = TimeSpan.FromMinutes(30);

    public MessageForwarderHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<MessageForwarderHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("---> MessageForwarderHostedService started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var messageForwarder = scope.ServiceProvider.GetRequiredService<IMessageForwarderService>();
                    await messageForwarder.Run();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "---> Error executing message forwarder. Retrying in {Interval}", interval);
                }

                await Task.Delay(interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            logger.LogInformation("---> MessageForwarderHostedService stopped.");
        }
    }
}