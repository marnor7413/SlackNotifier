using Google.Apis.Gmail.v1.Data;
using SN.Application.Dtos;

namespace SN.Application.Interfaces;

public interface IGmailFetchService
{
    Task<List<EmailInfo>> CheckForEmails();
    Task<MessagePartBody> DownloadAttachment(string messageId, string attachmentId);
}