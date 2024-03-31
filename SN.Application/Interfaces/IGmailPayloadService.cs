using Google.Apis.Gmail.v1.Data;
using MailService.Infrastructure.EmailService;

namespace MailService.Infrastructure.Extensions;

public interface IGmailPayloadService
{
    IEnumerable<MessagePart> GetAttachmentData(IList<MessagePart> parts);
    Task<List<FileAttachment>> GetAttachments(string messageId, IEnumerable<MessagePart> gmailAttachmentData);
    string GetText(MessagePart payload);
}