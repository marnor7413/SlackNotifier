using System.Text;

namespace MailService.Infrastructure.EmailService;

public record EmailInfo(int Id, string Date, string From, string Subject, string PlainTextBody, string HtmlBody) 
{
    public List<FileAttachment> FileAttachments { get; init; } = new List<FileAttachment>();

    public bool Validate()
    {
        if (Id < 0) return false;
        if (string.IsNullOrWhiteSpace(From)) return false;
        if (PlainTextBody is null && HtmlBody is null) return false;
        if (!DateTime.TryParse(Date, out var parsatDatum)) return false;

        return true;
    }

    public EmailInfo UpdatePlainText(string text) => this with { PlainTextBody = text };
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

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(FileName)) return false;
        if (string.IsNullOrWhiteSpace(fileType)) return false;
        if (string.IsNullOrWhiteSpace(Data)) return false;

        return true;
    }
}