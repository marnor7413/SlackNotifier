using AutoFixture;
using FluentAssertions;
using MailService.Infrastructure.EmailService;
using System.Text.Json.Nodes;
using System.Text;

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
    [InlineData("2024-05-25", true)]
    [InlineData("2024-05-25asdf", false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
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
    public void WhenUpdatingMessageBody_AndAnUpdatedObjectIsReturned_ItContainsCorrectInfo()
    {
        // Assign
        var SUT = new EmailInfo(id, date, from, subject, text, html);
        var newText = Fixture.Create<string>();
        var newHtmlText = Fixture.Create<string>();

        // Act
        var result = SUT.SetMessageBody(newText, newHtmlText);

        // Assert
        result.PlainTextBody.Should().Be(newText);
        result.HtmlBody.Should().Be(newHtmlText);
    }

    [Fact]
    public void WhenCreatingRequestBodyForSlack_AndObjectIsReturned_ItIsCorrectType()
    {
        // Assign
        var SUT = new EmailInfo(id, date, from, subject, text, html);

        // Act
        var result = SUT.ToSlackFormattedStringContent(Fixture.Create<string>());

        // Assert
        result.GetType().Should().Be(typeof(StringContent));
    }

    [Fact]
    public void WhenCreatingRequestBodyForSlack_AndObjectIsReturned_ItHasCorrectContentHeader()
    {
        // Assign
        const string Expected = "application/json";
        var SUT = new EmailInfo(id, date, from, subject, text, html);

        // Act
        var result = SUT.ToSlackFormattedStringContent(Fixture.Create<string>());

        // Assert
        result.Headers.ContentType.MediaType.Should().Be(Expected);
    }

    [Fact]
    public async Task WhenCreatingRequestBodyForSlack_AndObjectIsReturned_ItIsGeneratedCorrectly()
    {
        // Assign
        var SUT = new EmailInfo(id, date, from, subject, text, html);
        string channel = Fixture.Create<string>();
        var expected = GenerateJsonTextObject(channel, date, from, subject, text);

        // Act
        var result = SUT.ToSlackFormattedStringContent(channel);

        // Assert
        var jsonText = await result.ReadAsStringAsync();
        jsonText.Should().Be(expected);
    }

    [Theory]
    [InlineData("abc 123 <abc.123@anymail.com>", "abc 123 <mailto:abc.123@anymail.com|abc.123@anymail.com>")]
    [InlineData("abc 123", "abc 123")]
    public async Task WhenCreatingRequestBodyForSlack_AndDifferentFormatsInFrom_ItIsGeneratedCorrectly(string indata, string expectedFrom)
    {
        // Assign
        var SUT = new EmailInfo(id, date, indata, subject, text, html);
        string channel = Fixture.Create<string>();
        var expected = GenerateJsonTextObject(channel, date, expectedFrom, subject, text);

        // Act
        var result = SUT.ToSlackFormattedStringContent(channel);

        // Assert
        var jsonText = await result.ReadAsStringAsync();
        jsonText.Should().Be(expected);
    }

    private string GenerateJsonTextObject(string channel, string date, string from, string subject, string textMessage)
    {
        var text = new StringBuilder();
        text.AppendLine($"*Skickat: {DateTime.Parse(date).ToLocalTime()}*");
        text.AppendLine($"*Från: {from}*");
        text.AppendLine($"*Ämne: {subject}*");
        text.AppendLine(textMessage);

        var json = new JsonObject
        {
            { "channel", channel },
            { "text", text.ToString() }
        };

        return json.ToString();
    }
}