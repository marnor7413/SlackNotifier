using Google.Apis.Auth.OAuth2;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
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

    public GmailImapService(IIOService iOService, IConfiguration configuration)
    {
        googleCredentialsFilename = configuration.GetSection(AppsettingsKey).Value;
        this.iOService = iOService;
    }

    public async Task<List<MimeMessage>> DownloadEmails()
    {
        var credentials = iOService.ReadFileFromDisk(Directory.GetCurrentDirectory(), googleCredentialsFilename);
        var secrets = JsonConvert.DeserializeObject<GoogleApplicationPasswordSecrets>(credentials);

        using var client = new ImapClient();
        await client.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(secrets.Email, secrets.Password);
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
