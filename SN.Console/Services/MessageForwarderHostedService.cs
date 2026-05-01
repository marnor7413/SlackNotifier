using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SN.Application.Interfaces;

namespace SN.ConsoleApp.Services;

public class MessageForwarderHostedService : BackgroundService
{
    private readonly IMessageForwarderService messageForwarder;
    private readonly ILogger<MessageForwarderHostedService> logger;
    private readonly TimeSpan interval = TimeSpan.FromMinutes(30);

    public MessageForwarderHostedService(IMessageForwarderService messageForwarder, ILogger<MessageForwarderHostedService> logger)
    {
        this.messageForwarder = messageForwarder;
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