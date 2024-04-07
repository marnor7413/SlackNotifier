using Google.Apis.Gmail.v1.Data;
using SN.Application.Dtos;

namespace SN.Application.Interfaces;

public interface IGmailPayloadService
{
    IEnumerable<MessagePart> GetAttachmentData(IList<MessagePart> parts);
    Task<List<FileAttachment>> GetAttachments(string messageId, IEnumerable<MessagePart> gmailAttachmentData);
    string GetText(MessagePart payload);
}