using System.Text;

namespace MailService.Infrastructure.EmailService;

public record EmailInfo(int Id, string Date, string From, string Subject, string PlainTextBody, string HtmlBody)
{
    public int Id { get; set; } = Id;
    public string Date { get; set; } = Date;
    public string From { get; set; } = From;
    public string Subject { get; set; } = Subject;
    public string PlainTextBody { get; set; } = PlainTextBody;
    public string HtmlBody { get; set; } = HtmlBody;
    public List<FileAttachment> FileAttachments { get; init; } = new List<FileAttachment>();
}

public record FileAttachment(string FileName, string fileType, string Description, string Data)
{
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