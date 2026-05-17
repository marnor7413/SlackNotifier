using Microsoft.Extensions.Logging;
using MimeKit;
using SN.Application.Dtos;
using SN.Application.Extensions;
using SN.Application.Interfaces;

namespace SN.Application.Services;

public class GmailApplicationPasswordService : IGmailInboxService
{
    public string strategy => "Headless";
    private readonly IGmailImapService gmailImapService;
    private readonly ILogger<GmailApplicationPasswordService> logger;

    public GmailApplicationPasswordService(IGmailImapService gmailImapService, ILogger<GmailApplicationPasswordService> logger)
    {
        this.gmailImapService = gmailImapService;
        this.logger = logger;
    }

    public async Task<List<EmailInfo>> CheckForEmails()
    {
        var emailInfos = new List<EmailInfo>();
        var messages = await gmailImapService.DownloadEmails();

        foreach (var email in messages)
        {
            var id = email.MessageId.ToIntOrDefault();
            var dateSent = GetLocalTimeZone(email);
            var from = email.From.Mailboxes.FirstOrDefault()?.ToString() ?? "Okänd avsändare";
            var subject = email.Subject ?? string.Empty;
            var plaintextBody = email.TextBody ?? string.Empty;
            var htmlBody = email.HtmlBody ?? string.Empty;
            var fileAttachments = GetAttachments(email);
            var emailInfo = new EmailInfo(id, dateSent, from, subject, plaintextBody, htmlBody);
            emailInfo.FileAttachments.AddRange(fileAttachments);
            if (!emailInfo.Validate())
            {
                logger.LogInformation("Email with ID {EmailId} failed validation and will be skipped.", emailInfo.Id);
                continue;
            }
            emailInfos.Add(emailInfo);
        }

        return emailInfos;
    }

    private static string GetLocalTimeZone(MimeMessage email)
    {
        return TimeZoneInfo
            .ConvertTimeBySystemTimeZoneId(email.Date.UtcDateTime, "Europe/Stockholm")
            .ToString("yyyy-MM-dd HH:mm:ss");
    }

    private static List<FileAttachment> GetAttachments(MimeMessage email)
    {
        return email.Attachments
            .OfType<MimePart>()
            .Select(part =>
            {
                using var stream = new MemoryStream();
                part.Content.DecodeTo(stream);
                var data = Convert.ToBase64String(stream.ToArray());

                return new FileAttachment(
                    FileName: part.FileName ?? string.Empty,
                    FileType: part.ContentType.MimeType ?? string.Empty,
                    Description: string.Empty,
                    Data: data
                );
            })
            .Where(a => a.Validate())
            .ToList();
    }
}
