namespace SN.Core.ValueObjects;

public record MimeType(string Name)
{
    public static MimeType Text { get; } = new("text/plain");
    public static MimeType Html { get; } = new("text/html");
    public static MimeType MultiPartMixed { get; } = new("multipart/mixed");
    public static MimeType MultiPartAlternative { get; } = new("multipart/alternative");
    public static MimeType IphonePagesFileformat { get; } = new("application/x-iwork-pages-sffpages");
    public static MimeType ImageJpeg { get; } = new("image/jpeg");
    public static MimeType Pdf { get; } = new("application/pdf");
    public static MimeType Bitmap { get; } = new("image/bmp"); 
    public static MimeType Word97_2003 { get; } = new("application/msword"); 
    public static MimeType MicrosoftWord { get; } = new("application/vnd.openxmlformats-officedocument.wordprocessingml.document"); 
    public static MimeType Gif { get; } = new("image/gif"); 
    public static MimeType Csv { get; } = new("text/csv"); 
    public static MimeType PortableNetworkGraphics { get; } = new("image/png"); 
    public static MimeType Powerpoint { get; } = new("application/vnd.ms-powerpoint"); 
    public static MimeType PowerpointOpenXml { get; } = new("application/vnd.openxmlformats-officedocument.presentationml.presentation"); 
    public static MimeType Excel { get; } = new("application/vnd.ms-excel"); 
    public static MimeType ExcelOpenXml { get; } = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"); 
    public static MimeType ExcelMacroEnabled { get; } = new("application/vnd.ms-excel.sheet.macroEnabled.12"); 
    public static MimeType Zip { get; } = new("application/zip"); 
    public static MimeType Tiff { get; } = new("image/tiff"); 
    public static MimeType RichText { get; } = new("application/rtf"); 
}
