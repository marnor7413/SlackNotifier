using Google.Apis.Gmail.v1.Data;
using SN.Application.Dtos;
using SN.Application.Interfaces;
using SN.Core.ValueObjects;
using System.Text;

namespace SN.Application.Services;

public class GmailPayloadService : IGmailPayloadService
{
    private readonly IGmailFetchService gmailFetchService;

    public GmailPayloadService(IGmailFetchService gmailFetchService)
    {
        this.gmailFetchService = gmailFetchService;
    }

    public IEnumerable<MessagePart> GetAttachmentData(IList<MessagePart> parts)
    {
        return parts.Where(x => FileExtension.SupportedSlackFileTypes.Contains(x.MimeType)
            && !string.IsNullOrWhiteSpace(x.Filename));
    }

    public async Task<List<FileAttachment>> GetAttachments(string messageId, IEnumerable<MessagePart> gmailAttachmentData)
    {
        var emailAttachments = new List<FileAttachment>();
        foreach (var item in gmailAttachmentData)
        {
            var fileType = FileExtension.FromMimeType(new MimeType(item.MimeType));

            var attachmentId = item.Body.AttachmentId;
            var attachPart = await gmailFetchService.DownloadAttachment(messageId, attachmentId);
            var attachment = new FileAttachment(item.Filename, fileType.Name, "", attachPart.Data);
            if (attachment.Validate())
            {
                emailAttachments.Add(attachment);
            }
        }

        return emailAttachments;
    }

    public string GetText(MessagePart payload)
    {
        if (EmailBodyTextExists(payload))
        {
            try
            {
                var base64String = FileAttachment.Base64UrlSafeStringToBase64Standard(payload.Body.Data);
                byte[] data = Convert.FromBase64String(base64String);

                return Encoding.UTF8.GetString(data);
            }
            catch (Exception)
            {
                // no implementation
            }

        }

        return string.Empty;
    }

    private static bool EmailBodyTextExists(MessagePart payload)
    {
        return payload.Body != null && !string.IsNullOrWhiteSpace(payload.Body.Data);
    }
}
