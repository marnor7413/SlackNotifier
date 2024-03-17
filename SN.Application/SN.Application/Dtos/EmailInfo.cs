using System.Text;

namespace MailService.Infrastructure.EmailService;

public class EmailInfo
{
    public int Id { get; set; }
    public string Date { get; set; }
    public string From { get; set; }
    public string Subject { get; set; }
    public string PlainTextBody { get; set; }
    public string HtmlBody { get; set; }
    public List<JpegAttachment> JpegAttachments { get; init; } = new List<JpegAttachment>();
}

public class JpegAttachment
{
    public string FileName { get; set; }
    public string Description { get; set; }
    public string Data { get; set; }

    public byte[] ToUTF8ByteArray()
    {
        var base64String = Data.Replace("-", "+").Replace("_", "/");//.Replace(" ", "=");

        return Encoding.UTF8.GetBytes(base64String);
    }

    public byte[] ToByteArray()
    {
        var base64String = Data.Replace("-", "+").Replace("_", "/");//.Replace(" ", "=");

        return Convert.FromBase64String(base64String);
    }
}