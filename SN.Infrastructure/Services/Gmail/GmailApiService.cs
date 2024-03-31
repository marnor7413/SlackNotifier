using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using MailService.Infrastructure.EmailService;
using System.Text;
using MailService.Infrastructure.Factories;
using MailService.Infrastructure.EmailServices;
using SN.Core.ValueObjects;
using MailService.Infrastructure.Extensions;

namespace SN.Infrastructure.Services.Gmail;

public class GmailApiService : IGmailApiService
{
    private readonly IGmailServiceFactoryOauth gmailServiceFactory;
    private readonly IMessageTypeService messageTypeService;
    private GmailService service;

    private const string AuthenticatedUser = "me";
    private const string FilterUnreadEmailsOnly = "is:unread";
    private const string InboxFolder = "INBOX";

    public GmailApiService(IGmailServiceFactoryOauth gmailServiceFactory, 
        IMessageTypeService messageTypeService)
    {
        this.gmailServiceFactory = gmailServiceFactory;
        this.messageTypeService = messageTypeService;
    }

    public async Task<List<EmailInfo>> CheckForEmails()
    {
        service = await gmailServiceFactory.CreateService();

        var emailListRequest = service.Users.Messages.List(AuthenticatedUser);
        emailListRequest.LabelIds = InboxFolder;
        emailListRequest.IncludeSpamTrash = false;
        emailListRequest.Q = FilterUnreadEmailsOnly;
        var emails = new List<EmailInfo>();

        int counter = 1;
        ListMessagesResponse emailListResponse;
        try
        {
            emailListResponse = await emailListRequest.ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
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

            if (message.Payload.Parts is null && message.Payload.Body is not null)
            {
                email = email.SetMessageBody(GetText(message.Payload), GetText(message.Payload));
            }
            else if (message.IsAPlainMessage())
            {
                email = email.SetMessageBody(
                    GetText(message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                    GetText(message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
            }
            else if (message.IsMessageWithStupidIphoneAttachment())
            {
                email = email.SetMessageBody(
                    GetText(message.Payload.Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name).Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                    GetText(message.Payload.Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name).Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
            }
            else if (message.IsMultiPartAlternativeMessage())
            {
                email = email.SetMessageBody(
                    GetText(message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                    GetText(message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
            }
            else if (message.IsMultiPartMixed()) // text + image attachment
            {
                var textObject = message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name);
                email = email.SetMessageBody(
                    GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                    GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
            }

            var gmailAttachmentData = GetAttachmentData(message.Payload.Parts);
            if(gmailAttachmentData.Any()) 
            { 
                email.FileAttachments.AddRange(await GetAttachments(message.Id, gmailAttachmentData));
            }

            return email.Validate() ? email : null;
        }

        return null;
    }

    private IEnumerable<MessagePart> GetAttachmentData(IList<MessagePart> parts)
    {
        return parts.Where(x => FileExtension.SupportedSlackFileTypes.Contains(x.MimeType) 
            && !string.IsNullOrWhiteSpace(x.Filename));
    }

    private async Task<List<FileAttachment>> GetAttachments(string messageId, IEnumerable<MessagePart> gmailAttachmentData)
    {
        var emailAttachments = new List<FileAttachment>();
        foreach (var item in gmailAttachmentData)
        {
            var fileType = FileExtension.FromMimeType(new MimeType(item.MimeType));

            var attId = item.Body.AttachmentId;
            var attachPart = await service.Users.Messages.Attachments
                .Get(AuthenticatedUser, messageId, attId)
                .ExecuteAsync();
            var attachment = new FileAttachment(item.Filename, fileType.Name, "", attachPart.Data);
            if (attachment.Validate())
            {
                emailAttachments.Add(attachment);
            }
        }

        return emailAttachments;
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