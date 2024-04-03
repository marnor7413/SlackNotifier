using AutoFixture;
using FluentAssertions;
using Google.Apis.Gmail.v1.Data;
using MailService.Infrastructure.EmailService;
using MailService.Infrastructure.Factories;
using SN.Infrastructure.Services.Gmail;
using System.Text;

namespace SN.UnitTests.SN.Application;

public class FileAttachmentTests : BaseTests
{
    private readonly string gmailBase64String = "ZGF0YSBjb250ZW50";
    private readonly string originalText = "data content";
    private readonly string filename; 
    private readonly string fileType; 
    private readonly string description;
    private readonly string data;

    public FileAttachmentTests()
    {
        filename = "test.txt";
        fileType = "txt";
        description = Fixture.Create<string>();
        data = gmailBase64String;
    }

    [Fact]
    public void WhenConvertingToByteArray_AndDataIsBase64String_ItIsConvertedCorrectly()
    {
        // Assign
        var SUT = new FileAttachment(filename, fileType, description, data);

        // Act
        var result = SUT.ToByteArray();

        // Assert
        string utfString = Encoding.UTF8.GetString(result, 0, result.Length);
        Assert.Equal(originalText, utfString);
    }

    [Theory]
    [InlineData("abc123", true)]
    [InlineData(" ", false)]
    [InlineData("", false)]
    public void WhenValideate_AndFilenameIsValidated_CorrectValueIsReturned(string filename, bool expected)
    {
        // Assign
        var SUT = new FileAttachment(filename, fileType, description, data);

        // Act
        var result = SUT.Validate();

        // Assert
        result.Should().Be(expected);
    }


    [Theory]
    [InlineData("abc123", true)]
    [InlineData(" ", false)]
    [InlineData("", false)]
    public void WhenValideate_AndFiletypeIsValidated_CorrectValueIsReturned(string fileType, bool expected)
    {
        // Assign
        var SUT = new FileAttachment(filename, fileType, description, data);

        // Act
        var result = SUT.Validate();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("abc123", true)]
    [InlineData(" ", false)]
    [InlineData("", false)]
    public void WhenValideate_AndDataIsValidated_CorrectValueIsReturned(string data, bool expected)
    {
        // Assign
        var SUT = new FileAttachment(filename, fileType, description, data);

        // Act
        var result = SUT.Validate();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertFromBase64UrlToBase64Standard_Base64UrlSafeText_IsConvertedCorrectly()
    {
        // Assign
        const string base64UrlEncodedData = "PDw_Pz8-Pg  ";
        const string expected = "PDw/Pz8+Pg==";

        // Act
        var result = FileAttachment.Base64UrlSafeStringToBase64Standard(base64UrlEncodedData);

        // Assert
        result.Should().Be(expected);
    }
}
