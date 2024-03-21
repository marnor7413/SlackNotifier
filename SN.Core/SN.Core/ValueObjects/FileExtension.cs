namespace SN.Core.ValueObjects;

public record FileExtension(string Name)
{
    public static MimeType Jpeg { get; } = new("jpg");
    public static MimeType Pdf { get; } = new("pdf");
}