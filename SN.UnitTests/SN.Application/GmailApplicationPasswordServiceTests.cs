using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using NSubstitute;
using SN.Application.Dtos;
using SN.Application.Extensions;
using SN.Application.Interfaces;
using SN.Application.Services;
using System.Text;

namespace SN.UnitTests.SN.Application;

public class GmailApplicationPasswordServiceTests : BaseTests
{
    [Fact]
    public void StrategyProperty_WhenCalled_ReturnsCorrectValue()
    {
        // Assign
        var imapService = Substitute.For<IGmailImapService>();
        var SUT = new GmailApplicationPasswordService(imapService, NullLogger<GmailApplicationPasswordService>.Instance);

        // Act & Assert
        SUT.strategy.Should().Be("Headless");
    }

    [Fact]
    public async Task WhenCheckingForEmail_AndEmailExists_MethodReturnsCorrectValues()
    {
        // Assign
        var imapService = Substitute.For<IGmailImapService>();
        MimeMessage email = MockEmail();
        imapService.DownloadEmails().Returns(new List<MimeMessage> { email });
        var expectedObject = ExtractDataFromEmail(email);

        var SUT = new GmailApplicationPasswordService(imapService, NullLogger<GmailApplicationPasswordService>.Instance);

        // Act
        var result = (await SUT.CheckForEmails()).Single();

        // Assert
        result.Id.Should().Be(expectedObject.Id);
        result.Date.Should().Be(expectedObject.Date);
        result.From.Should().Be(expectedObject.From);
        result.Subject.Should().Be(expectedObject.Subject);
        result.HtmlBody.Should().Be(expectedObject.HtmlBody);
        result.PlainTextBody.Should().Be(expectedObject.PlainTextBody);
        
        result.FileAttachments
            .Single().FileName
            .Should()
            .Be(expectedObject.FileAttachments.Single().FileName);

        result.FileAttachments
            .Single().Data
            .Should()
            .Be(expectedObject.FileAttachments.Single().Data);

        result.FileAttachments
            .Single().FileType
            .Should()
            .Be(expectedObject.FileAttachments.Single().FileType);

        result.FileAttachments
            .Single().Description
            .Should()
            .Be(expectedObject.FileAttachments.Single().Description);
    }

    [Fact]
    public async Task WhenValidatingEmail_AndIdIsInvalid_EmailIsIgnoredAndEmptyListIsReturned()
    {
        // Assign
        var imapService = Substitute.For<IGmailImapService>();
        MimeMessage email = MockEmail(id: "-1");
        imapService.DownloadEmails().Returns(new List<MimeMessage> { email });
        var expectedObject = ExtractDataFromEmail(email);

        var SUT = new GmailApplicationPasswordService(imapService, NullLogger<GmailApplicationPasswordService>.Instance);

        // Act
        var result = await SUT.CheckForEmails();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenValidatingEmail_AndEmailIsInvalid_LoggingIsPerformed()
    {
        // Assign
        var imapService = Substitute.For<IGmailImapService>();
        MimeMessage email = MockEmail(id: "-1");
        imapService.DownloadEmails().Returns(new List<MimeMessage> { email });
        var expectedObject = ExtractDataFromEmail(email);
        var logger = Substitute.For<ILogger<GmailApplicationPasswordService>>();

        var SUT = new GmailApplicationPasswordService(imapService, logger);

        // Act
        var result = await SUT.CheckForEmails();

        // Assert
        AssertLogReceived(1, LogLevel.Information, logger, $"Email with ID {email.MessageId} failed validation and will be skipped.");
    }

    [Fact]
    public async Task WhenEmailReceived_AndFromSenderIsMissing_ValueOkändAvsändareIsSetByDefault()
    {
        // Assign
        var imapService = Substitute.For<IGmailImapService>();
        MimeMessage email = MockEmail();
        email.From.Clear();
        imapService.DownloadEmails().Returns(new List<MimeMessage> { email });
        var expectedObject = ExtractDataFromEmail(email);

        var SUT = new GmailApplicationPasswordService(imapService, NullLogger<GmailApplicationPasswordService>.Instance);

        // Act
        var result = await SUT.CheckForEmails();

        // Assert
        result.Single().From.Should().Be("Okänd avsändare");
    }

    private void AssertLogReceived(
        int amount,
        LogLevel logLevel,
        ILogger<GmailApplicationPasswordService> logger,
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

    private static MimeMessage MockEmail(string id = "1")
    {
        var message = new MimeMessage();
        message.MessageId = id;
        message.Date = DateTimeOffset.Now;
        message.From.Add(new MailboxAddress("Martin Norén", "martinnoren@mymail.com"));
        message.Subject = "Any subject";

        var builder = new BodyBuilder
        {
            TextBody = "Hello world",
            HtmlBody = "<p>Hello world</p>"
        };

        var attachmentBytes = Encoding.UTF8.GetBytes("fake pdf content");
        builder.Attachments.Add("document.pdf", attachmentBytes, ContentType.Parse("application/pdf"));
        message.Body = builder.ToMessageBody();

        return message;
    }

    private static EmailInfo ExtractDataFromEmail(MimeMessage email)
    {
        var id = email.MessageId.ToIntOrDefault();
        var dateSent = GetLocalTimeZone(email);
        var from = email.From.Mailboxes.FirstOrDefault()?.ToString() ?? "Okänd avsändare";
        var subject = email.Subject ?? string.Empty;
        var plaintextBody = email.TextBody ?? string.Empty;
        var htmlBody = email.HtmlBody ?? string.Empty;
        var fileAttachments = GetAttachments(email);
        
        var result = new EmailInfo(id, dateSent, from, subject, plaintextBody, htmlBody);
        result.FileAttachments.AddRange(fileAttachments);

        return result;
    }

    private static string GetLocalTimeZone(MimeMessage email)
    {
        return TimeZoneInfo
            .ConvertTimeBySystemTimeZoneId(email.Date.UtcDateTime, "Europe/Stockholm")
            .ToString("yyyy-MM-dd HH:mm:ss");
    }

    private static List<FileAttachment> GetAttachments(MimeMessage email)
    {
        return email.Attachments
            .OfType<MimePart>()
            .Select(part =>
            {
                using var stream = new MemoryStream();
                part.Content.DecodeTo(stream);
                var data = Convert.ToBase64String(stream.ToArray());

                return new FileAttachment(
                    FileName: part.FileName ?? string.Empty,
                    FileType: part.ContentType.MimeType ?? string.Empty,
                    Description: string.Empty,
                    Data: data
                );
            })
            .Where(a => a.Validate())
            .ToList();
    }
}
