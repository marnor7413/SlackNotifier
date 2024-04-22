using SN.Application.Dtos;

namespace SN.Application.Interfaces;

public interface IGmailInboxService
{
    Task<List<EmailInfo>> CheckForEmails();
}