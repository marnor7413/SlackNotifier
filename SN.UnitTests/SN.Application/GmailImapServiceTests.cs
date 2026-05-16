using AutoFixture;
using FluentAssertions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Options;
using MimeKit;
using NSubstitute;
using SN.Application.Dtos;
using SN.Application.Options;
using SN.Application.Services;

namespace SN.UnitTests.SN.Application;

public class GmailImapServiceTests : BaseTests
{
    [Fact]
    public async Task WhenNoUnreadEmailFound_AndNothingIsDownloaded_EmptyListIsReturnedToCallingMethod()
    {
        // Arrange
        var (connectionClient, _, options) = MockInterfaces();
        var SUT = new GmailImapService(connectionClient, options);

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

        var SUT = new GmailImapService(connectionClient, options);
        var expected = new List<MimeMessage>() { emailResult };

        // Act
        var result = await SUT.DownloadEmails();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(expected);
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
