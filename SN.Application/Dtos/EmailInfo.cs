namespace MailService.Infrastructure.EmailService;

public record EmailInfo(int id, string date, string from, string subject, string plainTextBody, string htmlBody) 
{
    public List<FileAttachment> FileAttachments { get; init; } = new List<FileAttachment>();

    public bool Validate()
    {
        if (id < 0) return false;
        if (string.IsNullOrWhiteSpace(from)) return false;
        if (string.IsNullOrWhiteSpace(plainTextBody) && string.IsNullOrWhiteSpace(htmlBody)) return false;
        if (!DateTime.TryParse(date, out var parsatDatum)) return false;

        return true;
    }

    public EmailInfo UpdatePlainText(string text) => this with { plainTextBody = text };
}