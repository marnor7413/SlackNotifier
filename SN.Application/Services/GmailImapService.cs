using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Options;
using MimeKit;
using SN.Application.Dtos;
using SN.Application.Interfaces;
using SN.Application.Metrics;
using SN.Application.Options;
using System.Diagnostics;

namespace SN.Application.Services;

public class GmailImapService : IGmailImapService
{
    private readonly GmailImapSecretsOptions options;
    private readonly IImapConnectionClient imapConnectionClient;
    private readonly SlackNotifierMetrics metrics;

    public GmailImapService(
        IImapConnectionClient imapConnectionClient,
        IOptions<GmailImapSecretsOptions> options,
        SlackNotifierMetrics metrics)
    {
        this.options = options.Value;
        this.imapConnectionClient = imapConnectionClient;
        this.metrics = metrics;
    }

    public async Task<List<MimeMessage>> DownloadEmails()
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var secrets = new GoogleApplicationPasswordSecrets
            {
                Email = options.Email,
                Password = options.Password
            };
            using IImapClient client = await imapConnectionClient.ConnectAsync(secrets);

            List<MimeMessage> emails = new();
            var uids = await client.Inbox.SearchAsync(SearchQuery.NotSeen);
            foreach (var uid in uids)
            {
                emails.Add(await client.Inbox.GetMessageAsync(uid));
                await client.Inbox.SetFlagsAsync(uid, MessageFlags.Seen, true);
            }

            await imapConnectionClient.DisconnectAsync(client);
            metrics.ImapFetchSucceeded(emails.Count);

            return emails;
        }
        catch (Exception exception)
        {
            metrics.ImapFetchFailed(exception);
            throw;
        }
        finally
        {
            metrics.RecordImapFetchDuration(Stopwatch.GetElapsedTime(startTimestamp));
        }
    }
}
