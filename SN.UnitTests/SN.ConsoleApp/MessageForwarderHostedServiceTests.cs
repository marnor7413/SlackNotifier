using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SN.Application.Interfaces;
using SN.Application.Metrics;
using SN.ConsoleApp.Services;
using SN.UnitTests.Fakes;

namespace SN.UnitTests.SN.ConsoleApp;

public class MessageForwarderHostedServiceTests
{
    [Fact]
    public async Task WhenRunThrows_ThenRunFailureIsRecordedAndErrorIsLogged()
    {
        // Assign
        using var meterFactory = new TestMeterFactory();
        var metrics = new SlackNotifierMetrics(meterFactory);
        // Subscribes to slacknotifier.runs.failed and records every measurement from now on.
        using var runsFailedListener = new MetricCollector<long>(
            meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.runs.failed");

        var messageForwarder = Substitute.For<IMessageForwarderService>();
        messageForwarder.Run().ThrowsAsync(new InvalidOperationException("Simulated failure"));
        var services = new ServiceCollection();
        services.AddScoped(_ => messageForwarder);
        await using var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = Substitute.For<ILogger<MessageForwarderHostedService>>();
        using var SUT = new MessageForwarderHostedService(
            scopeFactory, 
            logger, 
            metrics);

        // Act
        await SUT.StartAsync(CancellationToken.None);
        await runsFailedListener.WaitForMeasurementsAsync(minCount: 1, timeout: TimeSpan.FromSeconds(10));
        await SUT.StopAsync(CancellationToken.None);

        // Assert
        var measurement = runsFailedListener.GetMeasurementSnapshot()
            .Should()
            .ContainSingle().Subject;

        measurement.Value.Should().Be(1);
        measurement.Tags["error.type"].Should().Be(typeof(InvalidOperationException).FullName);

        logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == "Log")
            .Count(call => call.GetArguments()[0] is LogLevel level && level == LogLevel.Error)
            .Should().Be(1);
    }
}