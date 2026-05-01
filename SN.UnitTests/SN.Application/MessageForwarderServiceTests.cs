using AutoFixture;
using FluentAssertions;
using NSubstitute;
using System.Text;
using SN.Application.Interfaces;
using SN.Application.Dtos;
using SN.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SN.UnitTests.SN.Application;

public class MessageForwarderServiceTests : BaseTests
{
    [Fact]
    public async Task WhenFetchFromGmail_AndNoEmails_ThenEarlyExitIsMade()
    {
        // Assign
        var gmailInboxService = SetupInboxService(Enumerable.Empty<EmailInfo>().ToList());
        var slackService = Fixture.Freeze<ISlackService>();
        var logger = Substitute.For<ILogger<MessageForwarderService>>();
        var SUT = new MessageForwarderService(gmailInboxService, slackService, logger);

        // Act
        await SUT.Run();

        // Assert
        AssertLogReceived(1, LogLevel.Information, logger);
        await slackService.Received(0).SendMessage(Arg.Any<List<EmailInfo>>());
    }

    [Fact]
    public async Task WhenFetchFromGmail_AndNEmailsExist_ThenSlackServiceIsCalledAndTrueIsReturned()
    {
        // Assign
        var gmailInboxService = SetupInboxService(Fixture.CreateMany<EmailInfo>(1).ToList());
        var slackService = Fixture.Freeze<ISlackService>();
        var logger = Substitute.For<ILogger<MessageForwarderService>>();
        var SUT = new MessageForwarderService(gmailInboxService, slackService, logger);

        // Act
        await SUT.Run();

        // Assert
        AssertLogReceived(1, LogLevel.Information, logger);
        await slackService.Received(1).SendMessage(Arg.Any<List<EmailInfo>>());
    }

    [Fact]
    public async Task WhenEmailPlaintextBodyIsFiltered_AndItContainsAnyAvastAdText_ThenItIsRemoved()
    {
        // Assign
        var unexpecedStrings = new List<string>()
        {
            "<https://www.avast.com/sig-email/?utm_source=email&utm_medium=signature&utm_campaign=sig-email&utm_content=webmail>",
            "> <https://www.avast.com/sig-email?utm_source=email&utm_medium=signature>",
            "> <#abc-123_45-a>",
            "<#abc123>",
            "Virus-free.",
            "Virusfritt.",
            "> www.avast.com",
            "www.avast.com",
        };
        var text = new StringBuilder();
        foreach (string s in unexpecedStrings) { text.AppendLine(s); }

        var gmailInboxService = SetupInboxService(Fixture.Build<EmailInfo>()
                .With(x => x.PlainTextBody, text.ToString())
                .CreateMany(1)
            .ToList());

        var slackService = Fixture.Freeze<ISlackService>();
        List<EmailInfo> capturedMessages = null;
        await slackService.SendMessage(Arg.Do<List<EmailInfo>>(x => capturedMessages = x));

        var logger = Substitute.For<ILogger<MessageForwarderService>>();
        var SUT = new MessageForwarderService(gmailInboxService, slackService, NullLogger<MessageForwarderService>.Instance);

        // Act
        await SUT.Run();

        // Assert
        await slackService
            .Received(1)
            .SendMessage(Arg.Any<List<EmailInfo>>());
        CountOccurenciesOfTextStringsInText(unexpecedStrings, capturedMessages)
            .Should()
            .Be(0);
    }

    private void AssertLogReceived(int amount, LogLevel logLevel, ILogger<MessageForwarderService> logger)
    {
        logger.Received(amount).Log(
            logLevel,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }

    private IGmailInboxService SetupInboxService(List<EmailInfo> emailInfos)
    {
        var gmailInboxService = Fixture.Freeze<IGmailInboxService>();
        gmailInboxService.CheckForEmails()
            .Returns(emailInfos);

        return gmailInboxService;
    }

    private static int CountOccurenciesOfTextStringsInText(List<string> unexpecedStrings, List<EmailInfo> capturedMessages)
    {
        var matchingTexts = 0;
        foreach (var e in unexpecedStrings)
        {
            if (capturedMessages.Single().PlainTextBody.Contains(e))
            {
                matchingTexts++;
            }
        }

        return matchingTexts;
    }
}