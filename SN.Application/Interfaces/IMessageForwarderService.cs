namespace SN.Application.Interfaces;

public interface IMessageForwarderService
{
    Task<bool> Run();
}
