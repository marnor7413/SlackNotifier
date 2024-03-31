using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using MailService.Infrastructure.EmailService;
using MailService.Infrastructure.Factories;
using MailService.Infrastructure.EmailServices;
using SN.Core.ValueObjects;
using MailService.Infrastructure.Extensions;

namespace SN.Infrastructure.Services.Gmail;

public class GmailFetchService : IGmailFetchService
{
    private readonly IGmailServiceFactoryOauth gmailServiceFactory;
    private readonly IMessageTypeService messageTypeService;
    private readonly IGmailPayloadService payloadService;
    private GmailService gmailService;

    private const string FilterUnreadEmailsOnly = "is:unread";
    private const string InboxFolder = "INBOX";

    public GmailFetchService(IGmailServiceFactoryOauth gmailServiceFactory, 
        IMessageTypeService messageTypeService,
        IGmailPayloadService payloadService)
    {
        this.gmailServiceFactory = gmailServiceFactory;
        this.messageTypeService = messageTypeService;
        this.payloadService = payloadService;
    }

    public async Task<List<EmailInfo>> CheckForEmails()
    {
        gmailService = await gmailServiceFactory.GetService();

        var emailListRequest = gmailService.Users.Messages.List(GmailServiceFactoryOauth.AuthenticatedUser);
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
        var threads = emailListResponse
            .Messages?
            .GroupBy(x => x.ThreadId) ?? Enumerable.Empty<IGrouping<string, Message>>();

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
        Console.WriteLine("Fetching gmail messages.");
        var emailRequest = gmailService.Users.Messages.Get(GmailServiceFactoryOauth.AuthenticatedUser, emailId);
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
                await gmailService.Users.Messages
                    .Trash(GmailServiceFactoryOauth.AuthenticatedUser, emailId)
                    .ExecuteAsync();

                return null;
            }

            if (message.Payload.Parts is null && message.Payload.Body is not null)
            {
                email = email.SetMessageBody(
                    payloadService.GetText(message.Payload), 
                    payloadService.GetText(message.Payload));
            }
            else if (messageTypeService.IsAPlainMessage(message))
            {
                email = email.SetMessageBody(
                    payloadService.GetText(message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                    payloadService.GetText(message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
            }
            else if (messageTypeService.IsMessageWithStupidIphoneAttachment(message))
            {
                email = email.SetMessageBody(
                    payloadService.GetText(message.Payload.Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name).Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                    payloadService.GetText(message.Payload.Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name).Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
            }
            else if (messageTypeService.IsMultiPartAlternativeMessage(message))
            {
                email = email.SetMessageBody(
                    payloadService.GetText(message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                    payloadService.GetText(message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
            }
            else if (messageTypeService.IsMultiPartMixed(message)) // text + image attachment
            {
                var textObject = message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name);
                email = email.SetMessageBody(
                    payloadService.GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                    payloadService.GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
            }

            var gmailAttachmentData = payloadService.GetAttachmentData(message.Payload.Parts);
            if(gmailAttachmentData.Any()) 
            { 
                email.FileAttachments.AddRange(await payloadService.GetAttachments(message.Id, gmailAttachmentData));
            }

            return email.Validate() ? email : null;
        }

        return null;
    }

    private async Task ToggleMessageToRead(string emailId)
    {
        var modifyRequest = new ModifyMessageRequest
        {
            RemoveLabelIds = new List<string> { "UNREAD" },
            AddLabelIds = null 
        };

        var modifyResponse = await gmailService.Users.Messages
            .Modify(modifyRequest, GmailServiceFactoryOauth.AuthenticatedUser, emailId)
            .ExecuteAsync();
    }
}