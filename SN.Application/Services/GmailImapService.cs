using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Options;
using MimeKit;
using SN.Application.Dtos;
using SN.Application.Interfaces;
using SN.Application.Options;

namespace SN.Application.Services;

public class GmailImapService : IGmailImapService
{
    private readonly List<MimeMessage> emails = new();
    private readonly GmailImapSecretsOptions options;
    private readonly IImapConnectionClient imapConnectionClient;

    public GmailImapService(
        IImapConnectionClient imapConnectionClient,
        IOptions<GmailImapSecretsOptions> options)
    {
        this.options = options.Value;
        this.imapConnectionClient = imapConnectionClient;
    }

    public async Task<List<MimeMessage>> DownloadEmails()
    {
        var secrets = new GoogleApplicationPasswordSecrets
        {
            Email = options.Email,
            Password = options.Password
        };
        using IImapClient client = await imapConnectionClient.ConnectAsync(secrets);

        var uids = await client.Inbox.SearchAsync(SearchQuery.NotSeen);
        foreach (var uid in uids)
        {
            emails.Add(await client.Inbox.GetMessageAsync(uid));
            await client.Inbox.SetFlagsAsync(uid, MessageFlags.Seen, true);
        }

        await imapConnectionClient.DisconnectAsync(client);

        return emails;
    }
}
