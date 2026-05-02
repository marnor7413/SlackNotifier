using Microsoft.Extensions.Logging;
using SN.Application.Dtos;
using SN.Application.Interfaces;
using System.Text.RegularExpressions;

namespace SN.Application.Services;

public class MessageForwarderService : IMessageForwarderService
{
    private readonly IGmailInboxService gmailInboxService;
    private readonly ISlackService slackService;
    private readonly ILogger<IMessageForwarderService> logger;

    public MessageForwarderService(
        IGmailInboxService gmailInboxService, 
        ISlackService slackService,
        ILogger<IMessageForwarderService> logger)
    {
        this.gmailInboxService = gmailInboxService;
        this.slackService = slackService;
        this.logger = logger;
    }

    public async Task Run()
    {
        var emails = await gmailInboxService.CheckForEmails();

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
