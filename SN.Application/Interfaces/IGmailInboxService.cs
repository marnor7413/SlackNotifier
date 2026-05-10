using SN.Application.Dtos;

namespace SN.Application.Interfaces;

public interface IGmailInboxService
{
    string strategy { get; }

    Task<List<EmailInfo>> CheckForEmails();
}