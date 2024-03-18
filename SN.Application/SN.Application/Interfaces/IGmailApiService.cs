using MailService.Infrastructure.EmailService;

namespace MailService.Infrastructure.EmailServices
{
    public interface IGmailApiService
    {
        Task<List<EmailInfo>> CheckForEmails();
    }
}