using SN.Application.Dtos;
using SN.Application.Interfaces;

namespace SN.Application.Services;

public class GmailApplicationPasswordService : IGmailInboxService
{
    public string strategy => "Headless";

    private readonly IGmailImapService gmailImapService;

    public GmailApplicationPasswordService(IGmailImapService gmailImapService)
    {
        this.gmailImapService = gmailImapService;
    }

    public Task<List<EmailInfo>> CheckForEmails()
    {
        var messages = gmailImapService.DownloadEmails();

        throw new NotImplementedException();
    }
}
