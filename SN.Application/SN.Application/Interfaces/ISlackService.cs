using MailService.Infrastructure.EmailService;

namespace MailService.Infrastructure.SlackServices
{
    public interface ISlackService
    {
        Task SendMessage(List<EmailInfo> message);
    }
}