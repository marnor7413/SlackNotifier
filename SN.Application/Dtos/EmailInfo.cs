namespace MailService.Infrastructure.EmailService;

public record EmailInfo(int Id, string Date, string From, string Subject, string PlainTextBody, string HtmlBody) 
{
    public List<FileAttachment> FileAttachments { get; init; } = new List<FileAttachment>();

    public bool Validate()
    {
        if (Id < 0) return false;
        if (string.IsNullOrWhiteSpace(From)) return false;
        if (string.IsNullOrWhiteSpace(PlainTextBody) && string.IsNullOrWhiteSpace(HtmlBody)) return false;
        if (!DateTime.TryParse(Date, out var parsatDatum)) return false;

        return true;
    }

    public EmailInfo UpdatePlainText(string text) => this with { PlainTextBody = text };
}