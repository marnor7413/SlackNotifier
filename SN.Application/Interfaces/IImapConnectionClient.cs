using MailKit.Net.Imap;
using SN.Application.Dtos;

namespace SN.Application.Services;

public interface IImapConnectionClient
{
    Task<IImapClient> ConnectAsync(GoogleApplicationPasswordSecrets secrets);
    Task DisconnectAsync(IImapClient client);
}
