using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using SN.Application.Dtos;

namespace SN.Application.Services;

public class ImapConnectionClient : IImapConnectionClient
{
    public async Task<IImapClient> ConnectAsync(GoogleApplicationPasswordSecrets secrets)
    {
        var client = new ImapClient();
        await client.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(secrets.Email, secrets.Password);
        await client.Inbox.OpenAsync(FolderAccess.ReadWrite);

        return client;
    }

    public async Task DisconnectAsync(IImapClient client)
    {
        await client.DisconnectAsync(true);
    }
}
