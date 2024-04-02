using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using MailService.Infrastructure.EmailService;
using MailService.Infrastructure.Extensions;
using MailService.Infrastructure.Factories;
using SN.Core.ValueObjects;
using System.Text;

namespace SN.Infrastructure.Services.Gmail;

public class GmailPayloadService : IGmailPayloadService
{
    private readonly IGmailServiceFactory gmailServiceFactoryOauth;
    private GmailService service;

    public GmailPayloadService(IGmailServiceFactory gmailServiceFactoryOauth)
    {
        this.gmailServiceFactoryOauth = gmailServiceFactoryOauth;
    }

    public IEnumerable<MessagePart> GetAttachmentData(IList<MessagePart> parts)
    {
        return parts.Where(x => FileExtension.SupportedSlackFileTypes.Contains(x.MimeType)
            && !string.IsNullOrWhiteSpace(x.Filename));
    }

    public async Task<List<FileAttachment>> GetAttachments(string messageId, IEnumerable<MessagePart> gmailAttachmentData)
    {
        service = await gmailServiceFactoryOauth.GetService();
        var emailAttachments = new List<FileAttachment>();
        foreach (var item in gmailAttachmentData)
        {
            var fileType = FileExtension.FromMimeType(new MimeType(item.MimeType));

            var attId = item.Body.AttachmentId;
            var attachPart = await service.Users.Messages.Attachments
                .Get(GmailServiceFactory.AuthenticatedUser, messageId, attId)
                .ExecuteAsync();
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
        if (payload.Body != null && payload.Body.Data != null)
        {
            var base64String = payload.Body.Data.Replace("-", "+").Replace("_", "/");
            byte[] data = Convert.FromBase64String(base64String);
            return Encoding.UTF8.GetString(data);
        }
        return string.Empty;
    }
}
