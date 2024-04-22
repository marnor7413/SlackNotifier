using SN.Application.Dtos;

namespace SN.Application.Interfaces;

public interface ISlackService
{
    Task<bool> SendMessage(List<EmailInfo> message);
}