using MimeKit;

namespace SN.Application.Interfaces;

public interface IGmailImapService
{
    Task<List<MimeMessage>> DownloadEmails();
}
