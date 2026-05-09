using AutoFixture;
using FluentAssertions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Configuration;
using MimeKit;
using NSubstitute;
using SN.Application.Dtos;
using SN.Application.Interfaces;
using SN.Application.Services;

namespace SN.UnitTests.SN.Application;

public class GmailImapServiceTests : BaseTests
{
    [Fact]
    public async Task WhenNoUnreadEmailFound_AndNothingIsDownloaded_EmptyListIsReturnedToCallingMethod()
    {
        // Arrange
        var (ioService, configuration, connectionClient) = MockInterfaces();

        var uids = Enumerable.Empty<UniqueId>().ToList();
        var client = Substitute.For<IImapClient>();
        client.Inbox
            .SearchAsync(Arg.Any<SearchQuery>())
            .Returns(uids);
        connectionClient
            .ConnectAsync(Arg.Any<GoogleApplicationPasswordSecrets>())
            .Returns(client);

        var SUT = new GmailImapService(ioService, configuration, connectionClient);

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
        var (ioService, configuration, connectionClient) = MockInterfaces();

        var uids = new List<UniqueId> { Fixture.Create<UniqueId>() }.As<IList<UniqueId>>();
        var emailResult = MockEmail();
        var client = Substitute.For<IImapClient>();
        client.Inbox
            .SearchAsync(Arg.Any<SearchQuery>())
            .Returns(uids);
        client.Inbox
            .GetMessageAsync(Arg.Any<UniqueId>())
            .Returns(emailResult);
        connectionClient
            .ConnectAsync(Arg.Any<GoogleApplicationPasswordSecrets>())
            .Returns(client);

        var SUT = new GmailImapService(ioService, configuration, connectionClient);
        var expected = new List<MimeMessage>() { emailResult };


        // Act
        var result = await SUT.DownloadEmails();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(expected);
    }

    public (IIOService, IConfiguration, IImapConnectionClient) MockInterfaces()
    {
        var ioService = Substitute.For<IIOService>();
        var configuration = Substitute.For<IConfiguration>();
        configuration["Appsettings:GoogleImapCredentialsFilename"].Returns(Fixture.Create<string>());
        var connectionClient = Substitute.For<IImapConnectionClient>();

        return (ioService, configuration, connectionClient);
    }
}
