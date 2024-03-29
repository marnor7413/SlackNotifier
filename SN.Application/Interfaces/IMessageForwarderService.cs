namespace MailService.Application.Interfaces;

public interface IMessageForwarderService
{
    Task<bool> Run();
}
