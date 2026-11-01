using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SN.Application.Dtos;
using SN.Application.Interfaces;
using SN.Application.Metrics;
using System.Text.RegularExpressions;

namespace SN.Application.Services;

public class MessageForwarderService : IMessageForwarderService
{
    private readonly Dictionary<string, IGmailInboxService> strategies;
    private readonly ISlackService slackService;
    private readonly ILogger<IMessageForwarderService> logger;
    private readonly SlackNotifierMetrics metrics;
    private readonly string strategy;

    public MessageForwarderService(IConfiguration configuration,
        IEnumerable<IGmailInboxService> gmailInboxServices,
        ISlackService slackService,
        ILogger<IMessageForwarderService> logger,
        SlackNotifierMetrics metrics)
    {
        strategies = gmailInboxServices.ToDictionary(x => x.strategy, x => x);
        strategy = configuration["GmailStrategy"];
        this.slackService = slackService;
        this.logger = logger;
        this.metrics = metrics;
    }

    public async Task Run()
    {
        var gmailService = strategies.GetValueOrDefault(strategy);
        if (gmailService is null)
        {
            metrics.RunFailedStrategyNotFound();
            logger.LogWarning($"---> Strategy '{strategy}' not found.");

            return;
        }

        var emails = await gmailService.CheckForEmails();
        if (!emails.Any())
        {
            logger.LogInformation("---> No new emails found.");

            return;
        }

        var cleanedText = RemoveAvastAd(emails);
        var forwardedSuccessfully = await slackService.SendMessage(cleanedText.OrderBy(x => x.Id).ToList());
        if (forwardedSuccessfully)
        {
            logger.LogInformation("---> {EmailCount} email(s) forwarded to Slack.", emails.Count);
        }
        else
        {
            logger.LogWarning(
                "---> Forwarding to Slack failed for at least one of {EmailCount} email(s). See earlier log entries for details.",
                emails.Count);
        }
    }

    private static List<EmailInfo> RemoveAvastAd(List<EmailInfo> emails)
    {
        var avastLinkPattern = @"<https://www.avast.com/sig-email?.*?>";
        var quotedAvastLinkPattern = @">\s*<https://www.avast.com/sig-email\?.*?>";
        var quotedAvastIdPattern = @">\s*<#[\w-]+>";
        var avastIdPattern = @"<#(.*?)>";
        var avastVirusFreeTextPattern = @"Virus-free.";
        var avastVirusFreeTextPatternSE = @"Virusfritt.";
        var quotedAvastComPattern = @">\s*www\.avast\.com";
        var avastComPattern = @"www\.avast\.com";

        for (int i = 0; i < emails.Count; i++)
        {
            var email = emails[i];
            emails[i] = email.SetMessageBody(Regex.Replace(email.PlainTextBody,
            $"{avastLinkPattern}|{quotedAvastLinkPattern}|{quotedAvastIdPattern}|{avastIdPattern}|{avastComPattern}|{quotedAvastComPattern}|{Regex.Escape(avastVirusFreeTextPattern)}|{Regex.Escape(avastVirusFreeTextPatternSE)}",
            string.Empty), email.HtmlBody);
        }

        return emails;
    }
}