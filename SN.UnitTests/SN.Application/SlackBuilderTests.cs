using FluentAssertions;
using SN.Application.Builders;

namespace SN.UnitTests.SN.Application;

public class SlackBuilderTests : BaseTests
{
    [Fact]
    public void FromSender_WhenSenderTextOnlyContainsEmailAddress_NameIsExtractedFromEmail()
    {
        // Assign
        var input = "my.name@email.com";
        var SUT = new SlackBlockBuilder();

        // Act
        var result = SUT.FromSender(input).As<SlackBlockBuilder>();

        // Assert
        result.CurrentSenderName
            .Should()
            .Be("*Från:* my.name");
    }

    [Fact]
    public void FromSender_WhenSenderTextOnlyContainsEmailAddress_EmailIsCorrectlyFormatted()
    {
        // Assign
        var input = "my.name@email.com";
        var SUT = new SlackBlockBuilder();

        var result = SUT.FromSender(input).As<SlackBlockBuilder>();

        // Assert
        result.CurrentSenderEmail
            .Should()
            .Be("*Email:* <mailto:my.name@email.com|my.name@email.com>");
    }

    [Fact]
    public void FromSender_WhenSenderTextContainsNameAndEmailAddress_NameIsExtractedFromEmail()
    {
        // Assign
        var input = "\"my name\" <my.name@email.com>";
        var SUT = new SlackBlockBuilder();

        // Act
        var result = SUT.FromSender(input).As<SlackBlockBuilder>();

        // Assert
        result.CurrentSenderName
            .Should()
            .Be("*Från:* my name");
    }

    [Fact]
    public void FromSender_WhenSenderTextContainsNameAndEmailAddress_EmailIsCorrectlyFormatted()
    {
        // Assign
        var input = "\"my name\" <my.name@email.com>";
        var SUT = new SlackBlockBuilder();

        var result = SUT.FromSender(input).As<SlackBlockBuilder>();

        // Assert
        result.CurrentSenderEmail
            .Should()
            .Be("*Email:* <mailto:my.name@email.com|my.name@email.com>");
    }
}
