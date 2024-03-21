namespace SN.Core.ValueObjects;

public record MimeType(string Name)
{
    public static MimeType Text { get; } = new("text/plain");
    public static MimeType Html { get; } = new("text/html");
    public static MimeType MultiPartMixed { get; } = new("multipart/mixed");
    public static MimeType MultiPartAlternative { get; } = new("multipart/alternative");
    public static MimeType IphonePagesFileformat { get; } = new("application/x-iwork-pages-sffpages");
    public static MimeType ImageJpeg { get; } = new("image/jpeg");
    public static MimeType ApplicationPdf { get; } = new("application/pdf");
}
