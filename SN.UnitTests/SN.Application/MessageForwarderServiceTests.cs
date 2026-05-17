using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SN.Application.Dtos;
using SN.Application.Interfaces;
using SN.Application.Services;
using System.Text;

namespace SN.UnitTests.SN.Application;

public class MessageForwarderServiceTests : BaseTests
{
    [Fact]
    public async Task WhenStrategy_IsNotMatching_ThenEarlyExitIsMadeWithLogging()
    {
        // Assign
        var gmailServices = SetupInboxService(Enumerable.Empty<EmailInfo>().ToList());
        var slackService = Fixture.Freeze<ISlackService>();
        var logger = Substitute.For<ILogger<MessageForwarderService>>();
        var configuration = Substitute.For<IConfiguration>();
        var nonMatchingStrategy = Fixture.Create<string>();
        configuration["GmailStrategy"].Returns(nonMatchingStrategy);
        var SUT = new MessageForwarderService(configuration, gmailServices, slackService, logger);
        var expectedLogMessage = $"---> Strategy '{nonMatchingStrategy}' not found.";

        // Act
        await SUT.Run();

        // Assert
        AssertLogReceived(1, LogLevel.Warning, logger, expectedLogMessage);
        await slackService.Received(0).SendMessage(Arg.Any<List<EmailInfo>>());
    }

    [Fact]
    public async Task WhenFetchFromGmail_AndNoEmails_ThenEarlyExitIsMade()
    {
        // Assign
        var gmailServices = SetupInboxService(Enumerable.Empty<EmailInfo>().ToList());
        var slackService = Fixture.Freeze<ISlackService>();
        var logger = Substitute.For<ILogger<MessageForwarderService>>();
        var configuration = Substitute.For<IConfiguration>();
        configuration["GmailStrategy"].Returns("Headless");
        var SUT = new MessageForwarderService(configuration, gmailServices, slackService, logger);

        // Act
        await SUT.Run();

        // Assert
        AssertLogReceived(1, LogLevel.Information, logger, "---> No new emails found.");
        await slackService.Received(0).SendMessage(Arg.Any<List<EmailInfo>>());
    }

    [Fact]
    public async Task WhenFetchFromGmail_AndOneEmailExist_ThenSlackServiceIsCalledAndLoggingIsDone()
    {
        // Assign
        var emails = Fixture.Build<EmailInfo>()
                .CreateMany(1)
                .ToList();

        var gmailServices = SetupInboxService(emails);
        var slackService = Fixture.Freeze<ISlackService>();
        var logger = Substitute.For<ILogger<MessageForwarderService>>();
        var configuration = Substitute.For<IConfiguration>();
        configuration["GmailStrategy"].Returns("Headless");
        var SUT = new MessageForwarderService(configuration, gmailServices, slackService, logger);

        // Act
        await SUT.Run();

        // Assert
        AssertLogReceived(1, LogLevel.Information, logger, $"---> 1 email(s) forwarded to Slack.");
        await slackService.Received(1).SendMessage(Arg.Any<List<EmailInfo>>());
    }

    [Fact]
    public async Task WhenEmailFetchedOnce_AndNextPollFetchNewEmail_ThenOldEmailIsNotIncludedInTheCurrentEmailForwardSession()
    {
        // Assign
        var emailsFirstRound = Fixture.Build<EmailInfo>()
                .CreateMany(1)
                .ToList();
        var emailsSecondRound = Fixture.Build<EmailInfo>()
                .CreateMany(1)
                .ToList();

        var gmailApiService = Substitute.For<IGmailInboxService>();
        gmailApiService
            .strategy
            .Returns(Fixture.Create<string>());
        var gmalImapService = Substitute.For<IGmailInboxService>();
        gmailApiService
            .CheckForEmails()
            .Returns(emailsFirstRound, emailsSecondRound);
        gmailApiService
            .strategy
            .Returns("Headless");

        var gmailServices = new List<IGmailInboxService>
        {
            gmailApiService,
            gmalImapService
        };
        var slackService = Fixture.Freeze<ISlackService>();
        var logger = Substitute.For<ILogger<MessageForwarderService>>();
        var configuration = Substitute.For<IConfiguration>();
        configuration["GmailStrategy"].Returns("Headless");
        var SUT = new MessageForwarderService(configuration, gmailServices, slackService, logger);

        // Act
        await SUT.Run();
        await SUT.Run();

        // Assert
        AssertLogReceived(2, LogLevel.Information, logger, $"---> 1 email(s) forwarded to Slack.");
        await slackService.Received(2).SendMessage(Arg.Any<List<EmailInfo>>());
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
        foreach (string s in unexpecedStrings) 
        { 
            text.AppendLine(s); 
        }

        var emails = Fixture.Build<EmailInfo>()
                .With(x => x.PlainTextBody, text.ToString())
                .CreateMany(1)
                .ToList();
        var gmailServices = SetupInboxService(emails);
        
        var slackService = Fixture.Freeze<ISlackService>();
        List<EmailInfo> capturedMessages = null;
        await slackService.SendMessage(Arg.Do<List<EmailInfo>>(x => capturedMessages = x));
        var logger = Substitute.For<ILogger<MessageForwarderService>>();
        var configuration = Substitute.For<IConfiguration>();
        configuration["GmailStrategy"].Returns("Headless");
        var SUT = new MessageForwarderService(configuration, gmailServices, slackService, NullLogger<MessageForwarderService>.Instance);

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

    private void AssertLogReceived(
    int amount,
    LogLevel logLevel,
    ILogger<MessageForwarderService> logger,
    string expectedMessageContains = null)
    {
        var matchingCalls = logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == "Log")
            .Where(call =>
            {
                var args = call.GetArguments();
                return args[0] is LogLevel level && level == logLevel;
            })
            .ToList();

        matchingCalls.Should().HaveCount(amount);

        if (expectedMessageContains is not null)
        {
            var anyMatch = matchingCalls.Any(call =>
            {
                const int IndexOfLogMessage = 2;
                var state = call.GetArguments()[IndexOfLogMessage];
                return state?.ToString()?.Contains(expectedMessageContains) == true;
            });

            anyMatch
                .Should()
                .BeTrue(because: $"log message should contain '{expectedMessageContains}'");
        }
    }

    private IEnumerable<IGmailInboxService> SetupInboxService(List<EmailInfo> emailInfos)
    {
        var gmailApiService = Substitute.For<IGmailInboxService>();
        gmailApiService
            .strategy
            .Returns(Fixture.Create<string>());
        var gmalImapService = Substitute.For<IGmailInboxService>();
        gmailApiService
            .CheckForEmails()
            .Returns(emailInfos);
        gmailApiService
            .strategy
            .Returns("Headless");

        var result = new List<IGmailInboxService> 
        {
            gmailApiService,
            gmalImapService
        };

        return result;
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