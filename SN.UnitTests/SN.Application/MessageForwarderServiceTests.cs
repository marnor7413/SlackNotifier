using AutoFixture;
using AutoFixture.Kernel;
using FluentAssertions;
using MailService.Application.Services;
using MailService.Infrastructure.EmailServices;
using MailService.Infrastructure.Factories;
using MailService.Infrastructure.SlackServices;
using Microsoft.Extensions.Configuration;
using SN.Infrastructure.Services.Gmail;
using System.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AutoFixture.AutoNSubstitute;
using NSubstitute;
using MailService.Infrastructure.EmailService;

namespace SN.UnitTests.SN.Application
{
    public class MessageForwarderServiceTests : BaseTests
    {

        [Fact]
        public async Task WhenFetchFromGmail_AndNoEmails_ThenEarlyExitIsMade()
        {
            // Assign
            var hej = Fixture.Customize(new AutoNSubstituteCustomization());

            var gmailService = Fixture.Freeze<IGmailApiService>();
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
    }
}
