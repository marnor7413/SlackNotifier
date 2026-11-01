using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SN.Application.Interfaces;
using SN.Application.Metrics;

namespace SN.ConsoleApp.Services;

public class MessageForwarderHostedService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<MessageForwarderHostedService> logger;
    private readonly SlackNotifierMetrics metrics;
    private readonly TimeSpan interval = TimeSpan.FromMinutes(30);

    public MessageForwarderHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<MessageForwarderHostedService> logger,
        SlackNotifierMetrics metrics)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
        this.metrics = metrics;
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
                    metrics.RunFailed(ex);
                    logger.LogError(ex, "---> Error executing message forwarder. Retrying in {Interval}", interval);
                }

                // Outside the inner try, so a cancellation at shutdown is not counted as a failed run.
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