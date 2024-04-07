using MailService.Infrastructure.EmailService;

namespace MailService.Infrastructure.EmailServices
{
    Task<List<EmailInfo>> CheckForEmails();
    Task<MessagePartBody> DownloadAttachment(string messageId, string attachmentId);
}