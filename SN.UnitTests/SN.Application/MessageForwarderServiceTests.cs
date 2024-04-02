using AutoFixture;
using FluentAssertions;
using MailService.Application.Services;
using MailService.Infrastructure.EmailServices;
using MailService.Infrastructure.SlackServices;
using AutoFixture.AutoNSubstitute;
using NSubstitute;
using MailService.Infrastructure.EmailService;
using System.Text;

namespace SN.UnitTests.SN.Application;

public class MessageForwarderServiceTests : BaseTests
{
    public MessageForwarderServiceTests()
    {
        Fixture.Customize(new AutoNSubstituteCustomization());
    }

    [Fact]
    public async Task WhenFetchFromGmail_AndNoEmails_ThenEarlyExitIsMade()
    {
        // Assign
        var gmailService = Fixture.Freeze<IGmailFetchService>();
        gmailService.CheckForEmails()
            .Returns(Enumerable.Empty<EmailInfo>().ToList());

        var slackService = Fixture.Freeze<ISlackService>();

        var SUT = new MessageForwarderService(gmailService, slackService);

        // Act
        var result = await SUT.Run();

        // Assert
        result.Should().BeFalse();
        await slackService.Received(0).SendMessage(Arg.Any<List<EmailInfo>>());   
    }

    [Fact]
    public async Task WhenFetchFromGmail_AndNEmailsExist_ThenSlackServiceIsCalledAndTrueIsReturned()
    {
        // Assign
        var gmailService = Fixture.Freeze<IGmailFetchService>();
        gmailService.CheckForEmails()
            .Returns(Fixture.CreateMany<EmailInfo>(1).ToList());

        var slackService = Fixture.Freeze<ISlackService>();

        var SUT = new MessageForwarderService(gmailService, slackService);

        // Act
        var result = await SUT.Run();

        // Assert
        result.Should().BeTrue();
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

        var gmailService = Fixture.Freeze<IGmailFetchService>();
        gmailService.CheckForEmails()
            .Returns(Fixture.Build<EmailInfo>()
                .With(x => x.PlainTextBody, text.ToString())
                .CreateMany(1)
            .ToList());

        var slackService = Fixture.Freeze<ISlackService>();
        List<EmailInfo> capturedMessages = null;
        await slackService.SendMessage(Arg.Do<List<EmailInfo>>(x => capturedMessages = x));

        var SUT = new MessageForwarderService(gmailService, slackService);

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