namespace SN.Core.ValueObjects;

public record FileExtension(string Name)
{
    public static FileExtension Jpeg { get; } = new("jpg");
    public static FileExtension Pdf { get; } = new("pdf");
    public static FileExtension Txt { get; } = new("text");
    public static FileExtension Bitmap { get; } = new("bmp"); 
    public static FileExtension Word97_2003 { get; } = new("doc"); 
    public static FileExtension MicrosoftWord { get; } = new("docx");
    public static FileExtension Gif { get; } = new("gif");
    public static FileExtension Csv { get; } = new("csv");
    public static FileExtension PortableNetworkGraphics { get; } = new("png");
    public static FileExtension Powerpoint { get; } = new("ppt");
    public static FileExtension PowerpointOpenXml { get; } = new("pptx");
    public static FileExtension UnsupportedFileType { get; } = new("Unsupported filetype");

    public static FileExtension FromMimeType(MimeType mimeType)
    {
        return mimeType switch
        {
            var type when type == MimeType.ImageJpeg => Jpeg,
            var type when type == MimeType.Pdf => Pdf,
            var type when type == MimeType.Text => Txt,
            var type when type == MimeType.Bitmap => Bitmap,
            var type when type == MimeType.Word97_2003 => Word97_2003,
            var type when type == MimeType.MicrosoftWord => MicrosoftWord,
            var type when type == MimeType.Gif => Gif,
            var type when type == MimeType.Csv => Csv,
            var type when type == MimeType.Powerpoint => Powerpoint,
            var type when type == MimeType.PortableNetworkGraphics => PortableNetworkGraphics,
            _ => UnsupportedFileType
        };
    }
}