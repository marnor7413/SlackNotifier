using AutoFixture;
using FluentAssertions;
using Google.Apis.Gmail.v1.Data;
using SN.Application.Services;

namespace SN.UnitTests.SN.Application;

public class MessageTypeServiceTests : BaseTests
{
    [Fact]
    public void IsMultipartMixedMessage_ReturnsTrue()
    {
        // Assign
        var mainMimeType = "multipart/mixed";
        var subMimeType = "multipart/alternative";
        var message = Fixture.Build<Message>()
            .With(x => x.Payload, Fixture.Build<MessagePart>()
                .With(x => x.MimeType, mainMimeType)
                .With(x => x.Parts, Fixture.Build<MessagePart>()
                    .Without(x => x.Parts)
                    .With(x => x.MimeType, subMimeType)
                    .CreateMany(1)
                    .ToList())
                .Create())
            .Create();
        var SUT = new MessageTypeService();

        // Act
        var result = SUT.IsMultiPartMixed(message);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsMultipartAlternative_ReturnsTrue()
    {
        // Assign
        var mainMimeType = "multipart/alternative";
        var message = Fixture.Build<Message>()
            .With(x => x.Payload, Fixture.Build<MessagePart>()
                .With(x => x.MimeType, mainMimeType)
                .With(x => x.Parts, Fixture.Build<MessagePart>()
                    .Without(x => x.Parts)
                    .CreateMany(1)
                    .ToList())
                .Create())
            .Create();
        var SUT = new MessageTypeService();

        // Act
        var result = SUT.IsMultiPartAlternativeMessage(message);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsMessageWithIphonePagesAttachment_ReturnsTrue()
    {
        // Assign
        var mainMimeType = "multipart/mixed";
        var subMimeType = "application/x-iwork-pages-sffpages";
        var message = Fixture.Build<Message>()
            .With(x => x.Payload, Fixture.Build<MessagePart>()
                .With(x => x.MimeType, mainMimeType)
                .With(x => x.Parts, Fixture.Build<MessagePart>()
                    .Without(x => x.Parts)
                    .With(x => x.MimeType, subMimeType)
                    .CreateMany(1)
                    .ToList())
                .Create())
            .Create();
        var SUT = new MessageTypeService();

        // Act
        var result = SUT.IsMessageWithIphonePagesAttachment(message);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPlainTextMessage_ReturnsTrue()
    {
        // Assign
        var mainMimeType = "text/plain";
        var message = Fixture.Build<Message>()
            .With(x => x.Payload, Fixture.Build<MessagePart>()
                .With(x => x.MimeType, mainMimeType)
                .With(x => x.Parts, Fixture.Build<MessagePart>()
                    .Without(x => x.Parts)
                    .CreateMany(1)
                    .ToList())
                .Create())
            .Create();
        var SUT = new MessageTypeService();

        // Act
        var result = SUT.IsAPlainTextMessage(message);

        // Assert
        result.Should().BeTrue();
    }
}