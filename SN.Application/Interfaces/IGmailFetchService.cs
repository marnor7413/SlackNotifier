using MailService.Infrastructure.EmailService;

namespace MailService.Infrastructure.EmailServices
{
    public interface IGmailFetchService
    {
        Task<List<EmailInfo>> CheckForEmails();
    }
}