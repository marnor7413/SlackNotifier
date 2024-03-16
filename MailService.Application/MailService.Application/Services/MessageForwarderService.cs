﻿using MailService.Application.Interfaces;
using MailService.Infrastructure.EmailService;
using MailService.Infrastructure.EmailServices;
using MailService.Infrastructure.SlackServices;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace MailService.Application.Services;

public class MessageForwarderService : IMessageForwarderService
{
    private readonly IGmailApiService _gmailService;
    private readonly ISlackService _slackService;

    public MessageForwarderService(IGmailApiService gmailService, ISlackService slackService)
    {
        _gmailService = gmailService;
        _slackService = slackService;
    }

    public async Task Run()
    {
        var emails = await _gmailService.CheckForEmails();

        if (!emails.Any()) return;

        var garbageTextRemoved = RemoveAvastAd(emails);

        await _slackService.SendMessage(garbageTextRemoved.OrderBy(x => x.Id).ToList());
    }

    private static List<EmailInfo> RemoveAvastAd(List<EmailInfo> emails)
    {
        var avastLinkPattern = @"<https://www.avast.com/sig-email\?.*?>";
        var quotedAvastLinkPattern = @">\s*<https://www.avast.com/sig-email\?.*?>";
        var quotedAvastIdPattern = @">\s*<#[\w-]+>";
        var avastIdPattern = @"<#(.*?)>";
        var avastVirusFreeTextPattern = @"Virus-free.";
        var quotedAvastComPattern = @">\s*www\.avast\.com";
        var avastComPattern = @"www\.avast\.com";

        emails.ForEach(e => 
            e.PlainTextBody = Regex.Replace(e.PlainTextBody, 
            $"{avastLinkPattern}|{quotedAvastLinkPattern}|{quotedAvastIdPattern}|{avastIdPattern}|{avastComPattern}|{quotedAvastComPattern}|{Regex.Escape(avastVirusFreeTextPattern)}", 
            string.Empty));

        return emails;
    }
}
