using SN.Application.Dtos;

namespace SN.Application.Interfaces;

public interface ISlackService
{
    Task SendMessage(List<EmailInfo> message);
}