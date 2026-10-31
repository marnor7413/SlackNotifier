using AutoFixture;
using FluentAssertions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Options;
using MimeKit;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SN.Application.Dtos;
using SN.Application.Metrics;
using SN.Application.Options;
using SN.Application.Services;
using SN.UnitTests.Fakes;

namespace SN.UnitTests.SN.Application;

public class GmailImapServiceTests : BaseTests
{
    [Fact]
    public async Task WhenNoUnreadEmailFound_AndNothingIsDownloaded_EmptyListIsReturnedToCallingMethod()
    {
        // Arrange
        var (connectionClient, _, options) = MockInterfaces();
        var SUT = new GmailImapService(connectionClient, options, new SlackNotifierMetrics(new TestMeterFactory()));

        // Act
        var result = await SUT.DownloadEmails();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenUnreadEmailFound_AndIsDownloaded_ItIsReturnedToCallingMethod()
    {
        // Arrange
        var (connectionClient, client, options) = MockInterfaces();
        var uids = new List<UniqueId> { Fixture.Create<UniqueId>() }.As<IList<UniqueId>>();
        var emailResult = MockEmail();
        client.Inbox
            .SearchAsync(Arg.Any<SearchQuery>())
            .Returns(uids);
        client.Inbox
            .GetMessageAsync(Arg.Any<UniqueId>())
            .Returns(emailResult);

        var SUT = new GmailImapService(connectionClient, options, new SlackNotifierMetrics(new TestMeterFactory()));
        var expected = new List<MimeMessage>() { emailResult };

        // Act
        var result = await SUT.DownloadEmails();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task WhenEmailIsDownloaded_ThenFetchedCountAndLastSuccessAreRecorded()
    {
        // Arrange
        var (connectionClient, client, options) = MockInterfaces();
        var uids = new List<UniqueId> { Fixture.Create<UniqueId>() }.As<IList<UniqueId>>();
        client.Inbox.SearchAsync(Arg.Any<SearchQuery>()).Returns(uids);
        client.Inbox.GetMessageAsync(Arg.Any<UniqueId>()).Returns(MockEmail());

        using var meterFactory = new TestMeterFactory();
        var SUT = new GmailImapService(connectionClient, options, new SlackNotifierMetrics(meterFactory));
        using var fetched = new MetricCollector<long>(meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.imap.emails.fetched");
        using var lastSuccess = new MetricCollector<long>(meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.imap.last_success_timestamp");
        using var errors = new MetricCollector<long>(meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.errors");

        // Act
        await SUT.DownloadEmails();
        lastSuccess.RecordObservableInstruments();

        // Assert
        fetched.GetMeasurementSnapshot().Should().ContainSingle().Which.Value.Should().Be(1);
        lastSuccess.GetMeasurementSnapshot().Should().ContainSingle().Which.Value.Should().BePositive();
        errors.GetMeasurementSnapshot().Should().BeEmpty();
    }

    [Fact]
    public async Task WhenAuthenticationFails_ThenErrorIsRecordedAndExceptionIsRethrown()
    {
        // Arrange
        var (connectionClient, _, options) = MockInterfaces();
        connectionClient
            .ConnectAsync(Arg.Any<GoogleApplicationPasswordSecrets>())
            .ThrowsAsync(new MailKit.Security.AuthenticationException("Invalid credentials"));

        using var meterFactory = new TestMeterFactory();
        var SUT = new GmailImapService(connectionClient, options, new SlackNotifierMetrics(meterFactory));
        using var lastSuccess = new MetricCollector<long>(meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.imap.last_success_timestamp");
        using var errors = new MetricCollector<long>(meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.errors");

        // Act
        var act = () => SUT.DownloadEmails();

        // Assert
        await act.Should().ThrowAsync<MailKit.Security.AuthenticationException>();
        var error = errors.GetMeasurementSnapshot().Should().ContainSingle().Subject;
        error.Tags["target"].Should().Be("imap");
        error.Tags["error.type"].Should().Be("MailKit.Security.AuthenticationException");
        lastSuccess.RecordObservableInstruments();
        lastSuccess.GetMeasurementSnapshot().Should().BeEmpty();
    }

    private (IImapConnectionClient, IImapClient, IOptions<GmailImapSecretsOptions>) MockInterfaces()
    {
        var connectionClient = Substitute.For<IImapConnectionClient>();
        var uids = Enumerable.Empty<UniqueId>().ToList();
        var client = Substitute.For<IImapClient>();
        client.Inbox
            .SearchAsync(Arg.Any<SearchQuery>())
            .Returns(uids);
        connectionClient
            .ConnectAsync(Arg.Any<GoogleApplicationPasswordSecrets>())
            .Returns(client);

        var options = Options.Create(new GmailImapSecretsOptions
        {
            Email = "myname@mymail.com",
            Password = "password"
        });

        return (connectionClient, client, options);
    }
}
