using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using SN.Application.Interfaces;

namespace SN.Application.Services;

public class GmailImapService : IGmailImapService
{
    private List<MimeMessage> emails { get; set; } = new List<MimeMessage>();

    public async Task<List<MimeMessage>> DownloadEmails()
    {  
        using var client = new ImapClient();
        await client.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync("orgrytetorp@gmail.com", "ijmrpzudepmrldek");// <----- TODO: Move to configuration
        await client.Inbox.OpenAsync(FolderAccess.ReadWrite);
        var uids = await client.Inbox.SearchAsync(SearchQuery.NotSeen);

        foreach (var uid in uids)
        {
            emails.Add(await client.Inbox.GetMessageAsync(uid));
            await client.Inbox.SetFlagsAsync(uid, MessageFlags.Seen, true);
        }

        await client.DisconnectAsync(true);

        return emails;
    }
}
