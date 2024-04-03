using AutoFixture;
using FluentAssertions;
using Google.Apis.Gmail.v1.Data;
using MailService.Infrastructure.Factories;
using SN.Infrastructure.Services.Gmail;

namespace SN.UnitTests.SN.Infrastructure;

public class GmailPayloadServiceTests : BaseTests
{
    [Theory]
    [InlineData("text/plain", 1)]
    [InlineData("application/x-iwork-pages-sffpages", 1)]
    [InlineData("image/jpeg", 1)]
    [InlineData("application/pdf", 1)]
    [InlineData("image/bmp", 1)]
    [InlineData("application/msword", 1)]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", 1)]
    [InlineData("image/gif", 1)]
    [InlineData("text/csv", 1)]
    [InlineData("image/png", 1)]
    [InlineData("application/vnd.ms-powerpoint", 1)]
    [InlineData("application/vnd.openxmlformats-officedocument.presentationml.presentation", 1)]
    [InlineData("application/vnd.ms-excel", 1)]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 1)]
    [InlineData("application/vnd.ms-excel.sheet.macroEnabled.12", 1)]
    [InlineData("application/zip", 1)]
    [InlineData("image/tiff", 1)]
    [InlineData("application/rtf", 1)]
    [InlineData("invalid/MimeType", 0)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    public void GetAttachmentData_WhenMessagePartContainsCorrectMimeType_ItIsFetched(string mimeType, int expectedAmount)
    {
        // Assign
        var list = new List<MessagePart>();
        var correct = Fixture.Build<MessagePart>()
            .Without(x => x.Parts)
            .With(x => x.Filename, Fixture.Create<string>())
            .With(x => x.MimeType, mimeType)
            .Create();

        var randomIncorrect = Fixture.Build<MessagePart>()
            .Without(x => x.Parts)
            .CreateMany(1);
        var emptyFilename = Fixture.Build<MessagePart>()
            .Without(x => x.Parts)
            .With(x => x.Filename, string.Empty)
            .With(x => x.MimeType, mimeType)
            .Create();
        var filenameIsNull = Fixture.Build<MessagePart>()
            .Without(x => x.Parts)
            .Without(x => x.Filename)
            .With(x => x.MimeType, mimeType)
            .Create();

        list.AddRange(new[] { correct, emptyFilename, filenameIsNull }.Concat(randomIncorrect));


        var factory = Fixture.Freeze<IGmailServiceFactory>();
        var SUT = new GmailPayloadService(factory);

        // Act
        var result = SUT.GetAttachmentData(list);

        // Assert
        result.Count().Should().Be(expectedAmount);
    }

    [Fact]
    public void GetText_WhenBase64UrlSafeMessageDataExists_ATextStringIsReturned()
    {
        // Assign
        const string base64UrlEncodedData = "PDw_Pz8-Pg==";
        var payload = Fixture.Build<MessagePart>()
            .Without(x => x.Parts)
            .With(x => x.Body, Fixture.Build<MessagePartBody>()
                .With(x => x.Data, base64UrlEncodedData)
                .Create())
            .Create();
        
        var factory = Fixture.Freeze<IGmailServiceFactory>();
        var SUT = new GmailPayloadService(factory);
        var expected = "<<???>>";

        // Act
        var result = SUT.GetText(payload);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetText_WhenInvalidBase64MessageDataExists_EmptyStringIsReturned()
    {
        // Assign
        string invalidDataWithMissingPadding = "abc123";
        var payload = Fixture.Build<MessagePart>()
            .Without(x => x.Parts)
            .With(x => x.Body, Fixture.Build<MessagePartBody>()
                .With(x => x.Data, invalidDataWithMissingPadding)
                .Create())
            .Create();

        var factory = Fixture.Freeze<IGmailServiceFactory>();
        var SUT = new GmailPayloadService(factory);

        // Act
        var result = SUT.GetText(payload);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GetText_WhenMessagePartBodyDataIsNullOrEmpty_EmptyStringIsReturned(string data)
    {
        // Assign
        string invalidData = Fixture.Create<string>();
        var payload = Fixture.Build<MessagePart>()
            .Without(x => x.Parts)
            .With(x => x.Body, Fixture.Build<MessagePartBody>()
                .With(x => x.Data, data)
                .Create())
            .Create();

        var factory = Fixture.Freeze<IGmailServiceFactory>();
        var SUT = new GmailPayloadService(factory);

        // Act
        var result = SUT.GetText(payload);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetText_WhenMessagePartBodyPropertyMissing_EmptyStringIsReturned()
    {
        // Assign
        var payload = Fixture.Build<MessagePart>()
            .Without(x => x.Parts)
            .Without(x => x.Body)
            .Create();

        var factory = Fixture.Freeze<IGmailServiceFactory>();
        var SUT = new GmailPayloadService(factory);

        // Act
        var result = SUT.GetText(payload);

        // Assert
        result.Should().BeEmpty();
    }
}
