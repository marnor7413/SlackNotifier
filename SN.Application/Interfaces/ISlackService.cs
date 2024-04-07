using MailService.Infrastructure.EmailService;

namespace MailService.Infrastructure.SlackServices
{
    Task SendMessage(List<EmailInfo> message);
}