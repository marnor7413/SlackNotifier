using Google.Apis.Gmail.v1.Data;

namespace SN.Application.Interfaces;

public interface IGmailApiService
{
    Task<List<Message>> GetListOfMessages();
    Task<Message> DownloadEmail(string emailId);
    Task MoveMessageToTrash(string emailId);
    Task ToggleMessageToRead(string emailId);
    Task<MessagePartBody> DownloadAttachment(string messageId, string attachmentId);
}