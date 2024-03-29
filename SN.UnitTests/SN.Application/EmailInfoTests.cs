using AutoFixture;
using FluentAssertions;
using MailService.Infrastructure.EmailService;

namespace SN.UnitTests.SN.Application;

public class EmailInfoTests : BaseTests
{
    private readonly int id;
    private readonly string date;
    private readonly string from;
    private readonly string subject;
    private readonly string text;
    private readonly string html;

    public EmailInfoTests()
    {
        id = 1;
        date = DateTime.Now.ToString();
        from = Fixture.Create<string>();
        subject = Fixture.Create<string>();
        text = Fixture.Create<string>();
        html = Fixture.Create<string>();
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void WhenValidating_AndIdIsValidated_ReturnsCorrectValue(int id, bool expected)
    {
        // Assign
        var SUT = new EmailInfo(id, date, subject, from, text, html);

        // Act
        var result = SUT.Validate();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("2024-05-25asdf", false)]
    [InlineData("2024-05-25", true)]
    [InlineData("2024-05-25", true)]
    public void WhenValidating_AndDateIsValidated_ReturnsCorrectValue(string date, bool expected)
    {
        // Assign
        var SUT = new EmailInfo(id, date, subject, from, text, html);

        // Act
        var result = SUT.Validate();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
    [InlineData("abc123", true)]
    public void WhenValidating_AndFromIsValidated_ReturnsCorrectValue(string from, bool expected)
    {
        // Assign
        var SUT = new EmailInfo(id, date, from, subject, text, html);

        // Act
        var result = SUT.Validate();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("", "", false)]
    [InlineData(" ","", false)]
    [InlineData("", " ", false)]
    [InlineData(null, "", false)]
    [InlineData(null, " ", false)]
    [InlineData("", null, false)]
    [InlineData(" ", null, false)]
    [InlineData(null, null, false)]
    [InlineData("text", null, true)]
    [InlineData("text", " ", true)]
    [InlineData("text", "", true)]
    [InlineData(null, "html", true)]
    [InlineData("", "html", true)]
    [InlineData(" ", "html", true)]
    public void WhenValidating_AndTextAndHtmlIsValidated_ReturnsCorrectValue(string text, string html, bool expected)
    {
        // Assign
        var SUT = new EmailInfo(id, date, from, subject, text, html);

        // Act
        var result = SUT.Validate();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void WhenUpdatingPlaintext_AndUpdatedObjectIsReturned_ItContainsCorrectInfo()
    {
        // Assign
        var SUT = new EmailInfo(id, date, from, subject, text, html);
        var newText = Fixture.Create<string>();

        // Act
        var result = SUT.UpdatePlainText(newText);

        // Assert
        result.PlainTextBody.Should().Be(newText);
    }
}
