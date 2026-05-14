using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Newtonsoft.Json;
using SN.Application.Dtos;
using SN.Application.Interfaces;

namespace SN.Application.Services;

public class GmailImapService : IGmailImapService
{
    private readonly List<MimeMessage> emails = new List<MimeMessage>();
    private readonly string googleCredentialsFilename;
    private readonly string AppsettingsKey = "Appsettings:GoogleImapCredentialsFilename";
    private readonly IIOService iOService;
    private readonly IImapConnectionClient imapConnectionClient;

    public GmailImapService(IIOService iOService, IConfiguration configuration, IImapConnectionClient imapConnectionClient)
    {
        googleCredentialsFilename = configuration.GetSection(AppsettingsKey).Value;
        this.iOService = iOService;
        this.imapConnectionClient = imapConnectionClient;
    }

    public async Task<List<MimeMessage>> DownloadEmails()
    {
        var credentials = iOService.ReadFileFromDisk(Directory.GetCurrentDirectory(), googleCredentialsFilename);
        var secrets = JsonConvert.DeserializeObject<GoogleApplicationPasswordSecrets>(credentials);
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
