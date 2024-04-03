using FluentAssertions;
using SN.Core.ValueObjects;

namespace SN.UnitTests.SN.Core;

public class FileExtensionTests : BaseTests
{
    [Theory]
    [InlineData("image/jpeg", "jpg")]
    [InlineData("application/pdf", "pdf")]
    [InlineData("text/plain", "text")]
    [InlineData("image/bmp", "bmp")]
    [InlineData("application/msword", "doc")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "docx")]
    [InlineData("image/gif", "gif")]
    [InlineData("text/csv", "csv")]
    [InlineData("application/vnd.ms-powerpoint", "ppt")]
    [InlineData("application/vnd.ms-excel", "xls")]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx")]
    [InlineData("application/vnd.ms-excel.sheet.macroEnabled.12", "xlsm")]
    [InlineData("application/zip", "zip")]
    [InlineData("image/tiff", "tiff")]
    [InlineData("application/rtf", "rtf")]
    [InlineData("application/x-iwork-pages-sffpages", "pages")]
    [InlineData("image/png", "png")]
    [InlineData("Something else", "Unsupported filetype")]
    public void FromMimeType_WhenMimeTypeProvided_ReturnsCorrectFileExtensionProperty(string input, string expected)
    {
        // Assign & Act
        var result = FileExtension.FromMimeType(new MimeType(input));

        // Assert
        result.Name.Should().Be(expected);
    }

    [Fact]
    public void SupportedSlackFileTypes_ShouldBeCorrect()
    {
        // Assign & Act & Assert
        FileExtension.SupportedSlackFileTypes.Should().BeEquivalentTo(SupportedSlackFileFormats);
    }
}
