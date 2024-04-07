using Google.Apis.Gmail.v1.Data;
using SN.Application.Dtos;
using SN.Application.Interfaces;
using SN.Core.ValueObjects;
using System.Text;

namespace SN.Application.Services;

public class GmailPayloadService : IGmailPayloadService
{
    private readonly IGmailFetchService gmailFetchService;
    private readonly IMessageTypeService messageTypeService;

    public GmailPayloadService(IGmailFetchService gmailFetchService,
                IMessageTypeService messageTypeService)
    {
        this.gmailFetchService = gmailFetchService;
        this.messageTypeService = messageTypeService;
    }

    public async Task<List<EmailInfo>> CheckForEmails()
    {
        List<Message> emailListResponse = await gmailFetchService.GetListOfMessages();
        IEnumerable<IGrouping<string, Message>> threads = emailListResponse
            .GroupBy(x => x.ThreadId); ;

        int counter = 1;
        var emails = new List<EmailInfo>();
        foreach (var thread in threads)
        {
            var threadMaster = thread
                .Where(item => item.Id == item.ThreadId)
                .ToList();
            var threadChilds = thread
                .Except(threadMaster)
                .ToList();

            foreach (var item in threadMaster)
            {
                var email = await GetEmail(item.Id, counter++);

                if (email is null) continue;

                emails.Add(email);
            }

            foreach (var item in threadChilds)
            {
                var email = await GetEmail(item.Id, counter++);

                if (email is null) continue;

                emails.Add(email);
            }
        }

        return emails;
    }

    private async Task<EmailInfo> GetEmail(string emailId, int counter)
    {
        Message message;
        message = await gmailFetchService.DownloadEmail(emailId);

        if (message != null)
        {
            //await gmailFetchService.ToggleMessageToRead(emailId); //TODO: uncomment this line
            var email = new EmailInfo(
                counter,
                message.Payload.Headers.Single(x => x.Name == "Date").Value,
                message.Payload.Headers.Single(x => x.Name == "From").Value,
                message.Payload.Headers.Single(x => x.Name == "Subject").Value,
                string.Empty,
                string.Empty);

            if (email.From.StartsWith("Google") || email.From.StartsWith("The Gmail"))
            {
                await gmailFetchService.MoveMessageToTrash(emailId);

                return null;
            }

            if (message.Payload.Parts is null && message.Payload.Body is not null)
            {
                email = email.SetMessageBody(
                    GetText(message.Payload),
                    GetText(message.Payload));
            }
            else if (messageTypeService.IsMessageWithIphonePagesAttachment(message))
            {
                email = email.SetMessageBody(
                    GetText(message.Payload.Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name).Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                    GetText(message.Payload.Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name).Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
            }
            else if (messageTypeService.IsAPlainTextMessage(message) || messageTypeService.IsMultiPartAlternativeMessage(message))
            {
                email = email.SetMessageBody(
                    GetText(message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                    GetText(message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
            }
            else if (messageTypeService.IsMultiPartMixed(message))
            {
                var textObject = message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name);
                email = email.SetMessageBody(
                    GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                    GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
            }

            var gmailAttachmentData = GetAttachmentData(message.Payload.Parts);
            if (gmailAttachmentData.Any())
            {
                email.FileAttachments.AddRange(await GetAttachments(message.Id, gmailAttachmentData));
            }

            return email.Validate() ? email : null;
        }

        return null;
    }
    
    private async Task<List<FileAttachment>> GetAttachments(string messageId, IEnumerable<MessagePart> gmailAttachmentData)
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

    private IEnumerable<MessagePart> GetAttachmentData(IList<MessagePart> parts)
    {
        return parts.Where(x => FileExtension.SupportedSlackFileTypes.Contains(x.MimeType)
            && !string.IsNullOrWhiteSpace(x.Filename));
    }

    private string GetText(MessagePart payload)
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
