using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using MailService.Infrastructure.EmailService;
using MailService.Infrastructure.Extensions;
using System.Text;
using MailService.Infrastructure.Factories;
using MailService.Infrastructure.EmailServices;
using SN.Core.ValueObjects;

namespace SN.Infrastructure.Services.Gmail;

public class GmailApiService : IGmailApiService
{
    private GmailService service;

    private const string AuthenticatedUser = "me";
    private const string FilterUnreadEmailsOnly = "is:unread";
    private const string InboxFolder = "INBOX";

    private const string HeaderEncodingValueForBase64 = "base64";
    private const string HeaderEncodingValueContentTransferEncoding = "Content-Transfer-Encoding";

    private string base64String = string.Empty;

    public GmailApiService(IGmailClientFactory gmailClientFactory)
    {
        service = gmailClientFactory.CreateGmailClient();
    }

    public async Task<List<EmailInfo>> CheckForEmails()
    {
        var emailListRequest = service.Users.Messages.List(AuthenticatedUser);
        emailListRequest.LabelIds = InboxFolder;
        emailListRequest.IncludeSpamTrash = false;
        emailListRequest.Q = FilterUnreadEmailsOnly;
        var emails = new List<EmailInfo>();

        int counter = 1;
        var emailListResponse = await emailListRequest.ExecuteAsync();
        var threads = emailListResponse.Messages?.GroupBy(x => x.ThreadId) ?? Enumerable.Empty<IGrouping<string, Message>>();

        foreach (var thread in threads)
        {
            var threadMaster = thread.Where(item => item.Id == item.ThreadId).ToList();
            var threadChilds = thread.Except(threadMaster).ToList();

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
        Console.WriteLine("Fetching gmail messages.");
        var emailRequest = service.Users.Messages.Get(AuthenticatedUser, emailId);
        var message = await emailRequest.ExecuteAsync();
        Console.WriteLine("Message received.");


        if (message != null)
        {
            //await ToggleMessageToRead(emailId); //TODO: uncomment this line
            var email = new EmailInfo(
                counter, 
                message.Payload.Headers.Single(x => x.Name == "Date").Value, 
                message.Payload.Headers.Single(x => x.Name == "From").Value, 
                message.Payload.Headers.Single(x => x.Name == "Subject").Value, 
                string.Empty, 
                string.Empty);

            if (email.From.StartsWith("Google") || email.From.StartsWith("The Gmail"))
            {
                await service.Users.Messages
                    .Trash(AuthenticatedUser, emailId)
                    .ExecuteAsync();

                return null;
            }

            if (message.Payload.Parts == null && message.Payload.Body != null)
            {
                email.PlainTextBody = GetText(message.Payload);
                email.HtmlBody = GetText(message.Payload);
            }
            else if (IsAPlainMessage(message))
            {
                email.PlainTextBody = GetText(message.Payload.Parts
                    .SingleOrDefault(x => x.MimeType == MimeType.Text.Name));
                email.HtmlBody = GetText(message.Payload.Parts
                    .SingleOrDefault(x => x.MimeType == MimeType.Html.Name));
            }
            else if (IsMessageWithStupidIphoneAttachment(message))
            {
                email.PlainTextBody = GetText(message.Payload.Parts
                    .SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name)
                    .Parts.SingleOrDefault(x => x.MimeType == MimeType.Text.Name));
                email.HtmlBody = GetText(message.Payload.Parts
                    .SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name)
                    .Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name));
            }
            else if (IsMultiPartAlternativeMessage(message))
            {
                email.PlainTextBody = GetText(message.Payload.Parts
                    .SingleOrDefault(x => x.MimeType == MimeType.Text.Name));
                email.HtmlBody = GetText(message.Payload.Parts
                    .SingleOrDefault(x => x.MimeType == MimeType.Html.Name));
            }
            else if (IsMultiPartMixed(message)) // text + image attachment
            {
                var gmailAttachmentData = GetAttachmentData(message.Payload.Parts);
                var attachments = await GetAttachments(message.Id, gmailAttachmentData);
                email.FileAttachments.AddRange(attachments);
               
                var textObject = message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name);
                email.PlainTextBody = GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeType.Text.Name));
                email.HtmlBody = GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name));
            }

            return email;
        }

        return null;
    }

    private IEnumerable<MessagePart> GetAttachmentData(IList<MessagePart> parts)
    {
        var supportedFileTypes = new List<string>() 
        { 
            MimeType.ImageJpeg.Name,
            MimeType.ApplicationPdf.Name
        }.AsReadOnly();

        return parts.Where(x => supportedFileTypes.Contains(x.MimeType));
    }

    private async Task<List<FileAttachment>> GetAttachments(string messageId, IEnumerable<MessagePart> gmailAttachmentData)
    {
        var emailAttachments = new List<FileAttachment>();
        foreach (var item in gmailAttachmentData)
        {

            var fileType = item.MimeType switch
            {
                var compiletimeText when compiletimeText == MimeType.ImageJpeg.Name => FileExtension.Jpeg.Name,
                var compiletimeText when compiletimeText == MimeType.ApplicationPdf.Name => FileExtension.Pdf.Name,
                _ => string.Empty
            };
            
            var attId = item.Body.AttachmentId;
            var attachPart = await service.Users.Messages.Attachments
                .Get(AuthenticatedUser, messageId, attId)
                .ExecuteAsync();
            emailAttachments.Add(new FileAttachment(item.Filename, fileType, "", attachPart.Data));
        }

        return emailAttachments;
    }

    private bool IsMultiPartMixed(Message message)
    {
        return message.HasMimeType(MimeType.MultiPartMixed.Name) &&
            message.HasSubMimeType(MimeType.MultiPartAlternative.Name);
    }

    private bool IsMultiPartAlternativeMessage(Message message)
    {
        return message.HasMimeType(MimeType.MultiPartAlternative.Name) &&
            message.HasSubMimeType(MimeType.Text.Name) &&
            message.HasSubMimeType(MimeType.Html.Name) &&
            message.Payload.Parts.Count() == 2;
    }

    private bool IsMessageWithStupidIphoneAttachment(Message message)
    {
        return message.HasMimeType(MimeType.MultiPartMixed.Name) &&
            message.HasSubMimeType(MimeType.IphonePagesFileformat.Name);
    }

    private static bool IsAPlainMessage(Message message)
    {
        return message.HasMimeType(MimeType.Text.Name) &&
            message.HasSubMimeType(MimeType.Text.Name) &&
            message.HasSubMimeType(MimeType.Text.Name) &&
            message.Payload.Parts.Count() == 2;
    }

    private string GetText(MessagePart payload)
    {
        if (payload.Body != null && payload.Body.Data != null)
        {
            var base64String = payload.Body.Data.Replace("-", "+").Replace("_", "/");
            byte[] data = Convert.FromBase64String(base64String);
            return Encoding.UTF8.GetString(data);
        }
        return string.Empty;
    }

    private async Task ToggleMessageToRead(string emailId)
    {
        var modifyRequest = new ModifyMessageRequest
        {
            RemoveLabelIds = new List<string> { "UNREAD" }, // Remove the UNREAD label
            AddLabelIds = null // You can add other labels if needed
        };

        var modifyResponse = await service.Users.Messages
            .Modify(modifyRequest, AuthenticatedUser, emailId)
            .ExecuteAsync();
    }
}