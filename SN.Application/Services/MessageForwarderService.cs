using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SN.Application.Dtos;
using SN.Application.Interfaces;
using System.Text.RegularExpressions;

namespace SN.Application.Services;

public class MessageForwarderService : IMessageForwarderService
{
    private readonly Dictionary<string, IGmailInboxService> strategies;
    private readonly ISlackService slackService;
    private readonly ILogger<IMessageForwarderService> logger;
    private readonly string strategy;

    public MessageForwarderService(IConfiguration configuration,
        IEnumerable<IGmailInboxService> gmailInboxServices, 
        ISlackService slackService,
        ILogger<IMessageForwarderService> logger)
    {
        strategies = gmailInboxServices.ToDictionary(x => x.strategy, x => x);
        strategy = configuration["GmailStrategy"];
        this.slackService = slackService;
        this.logger = logger;
    }

    public async Task Run()
    {
        var gmailService = strategies.GetValueOrDefault(strategy);
        if (gmailService is null) 
        {
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
        await slackService.SendMessage(cleanedText.OrderBy(x => x.Id).ToList());
        logger.LogInformation($"---> {emails.Count} email(s) forwarded to Slack.");

        return;
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
