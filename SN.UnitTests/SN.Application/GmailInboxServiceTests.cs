using AutoFixture;
using Google.Apis.Gmail.v1.Data;
using NSubstitute;
using SN.Application.Interfaces;
using SN.Application.Services;
using SN.UnitTests.Factories;

namespace SN.UnitTests.SN.Application;

public class GmailInboxServiceTests : BaseTests
{
    [Fact]
    public async Task CheckForEmails_WhenEmailContainsHtmlAndPlainTextAndPdfAttachment_EmailMessageBuiltUponHtmlTextAndPdfAttachmentIsFetched()
    {
        // Assign
        var (gmailApiService, gmailPayloadService, messageTypeService) = SetupDependencies();
        var message = TestMessageFactory.TextHtmlMessageWithAttachment(Fixture);
        gmailApiService
            .GetListOfMessages()
            .Returns(Task.FromResult(Fixture.Build<Message>().Without(x => x.Payload).CreateMany(1).ToList()));
        gmailApiService
            .DownloadEmail(Arg.Any<string>())
            .Returns(Task.FromResult(message));

        var SUT = new GmailInboxService(gmailApiService, gmailPayloadService, messageTypeService);

        // Act
        var result = await SUT.CheckForEmails();

        // Assert
    }

    private (IGmailApiService, IGmailPayloadService, IMessageTypeService) SetupDependencies()
    {
        var gmailApiService = Fixture.Freeze<IGmailApiService>();
        var gmailPayloadService = Fixture.Freeze<IGmailPayloadService>();
        var messageTypeService = Fixture.Freeze<IMessageTypeService>();

        return (gmailApiService, gmailPayloadService, messageTypeService);
    }
}