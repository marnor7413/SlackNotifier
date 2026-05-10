using Google.Apis.Gmail.v1.Data;
using SN.Application.Dtos;
using SN.Application.Interfaces;
using SN.Core.ValueObjects;

namespace SN.Application.Services;

public class GmailOauth2Service : IGmailInboxService
{
    public string strategy => "BrowserAuthentication";

    private readonly IGmailApiService gmailApiService;
    private readonly IGmailPayloadService gmailPayloadService;
    private readonly IMessageTypeService messageTypeService;

    public GmailOauth2Service(IGmailApiService gmailApiService,
        IGmailPayloadService gmailPayloadService,
        IMessageTypeService messageTypeService)
    {
        this.gmailApiService = gmailApiService;
        this.gmailPayloadService = gmailPayloadService;
        this.messageTypeService = messageTypeService;
    }

    public async Task<List<EmailInfo>> CheckForEmails()
    {
        List<Message> emailListResponse = await gmailApiService.GetListOfMessages();
        int counter = 1;
        var emails = new List<EmailInfo>();
        foreach (var item in emailListResponse)
        {
            var email = await GetEmail(item.Id, counter++);
            if (email is null) continue;
            
            emails.Add(email);
        }

        return emails;
    }

    private async Task<EmailInfo> GetEmail(string emailId, int counter)
    {
        Message message = null;
        message = await gmailApiService.DownloadEmail(emailId);

        if (message != null)
        {
            await gmailApiService.ToggleMessageToRead(emailId);
            var email = new EmailInfo(
                counter,
                message.Payload.Headers.Single(x => x.Name == "Date").Value,
                message.Payload.Headers.Single(x => x.Name == "From").Value,
                message.Payload.Headers.Single(x => x.Name == "Subject").Value,
                string.Empty,
                string.Empty);

            if (email.From.StartsWith("Google") || email.From.StartsWith("The Gmail"))
            {
                await gmailApiService.MoveMessageToTrash(emailId);

                return null;
            }

            //var (plainText, htmlText) = ExtractTextFromMessage(message);
            //email.SetMessageBody(plainText, htmlText);

            if (message.Payload is { Parts: null, Body: not null } && message.Payload.MimeType == MimeType.Html.Name)
            {
                email = email.SetMessageBody(
                    gmailPayloadService.GetTextFromHtml(message.Payload),
                    gmailPayloadService.GetText(message.Payload));
            }
            else if (messageTypeService.IsMessageWithIphonePagesAttachment(message))
            {
                email = email.SetMessageBody(
                    gmailPayloadService.GetText(message.Payload.Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name).Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                    gmailPayloadService.GetText(message.Payload.Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name).Parts
                        .SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
            }
            else if (messageTypeService.IsAPlainTextMessage(message) || messageTypeService.IsMultiPartAlternativeMessage(message))
            {
                if (HasBothHtmlAndPlainTextMessageBody(message))
                {
                    email = email.SetMessageBody(
                        gmailPayloadService.GetTextFromHtml(message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name)),
                        gmailPayloadService.GetText(message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
                }
                else
                {
                    email = email.SetMessageBody(
                        gmailPayloadService.GetText(message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                        gmailPayloadService.GetText(message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
                }

            }
            else if (HasAttachmentOnly(message))
            {
                var textObject = message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name);
                email = email.SetMessageBody(
                    gmailPayloadService.GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                    gmailPayloadService.GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
            }
            else if (HasAttachmentAndEmbeddedImage(message))
            {
                //TODO: add support to download embedded files
                var textObject = message.Payload.Parts
                    .SingleOrDefault(x => x.MimeType == MimeType.MultiPartRelated.Name)?.Parts
                    .SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name);
                email = email.SetMessageBody(
                    gmailPayloadService.GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                    gmailPayloadService.GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
            }
            else if (HasEmbeddedFileInMessageBodyOnly(message))
            {
                var textObject = message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.MultiPartAlternative.Name);
                email = email.SetMessageBody(
                    gmailPayloadService.GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeType.Text.Name)),
                    gmailPayloadService.GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name)));
            }

            var gmailAttachmentData = gmailPayloadService.GetAttachmentData(message.Payload.Parts);
            if (gmailAttachmentData.Any())
            {
                email.FileAttachments.AddRange(await gmailPayloadService.GetAttachments(message.Id, gmailAttachmentData));
            }

            if (message.Payload.Parts is not null)
            {
                var relatedAttachments = message.Payload.Parts
                    .SingleOrDefault(x => x.MimeType == "multipart/related")?.Parts
                    .Where(x => x.MimeType != "multipart/alternative") 
                    ?? Enumerable.Empty<MessagePart>();

                if (relatedAttachments.Any())
                {
                    email.RelatedFileAttachments
                        .AddRange(await gmailPayloadService.GetAttachments(message.Id, relatedAttachments));
                }
            }

            return email.Validate() ? email : null;
        }

        return null;
    }

    private (string plainText, string htmlText) ExtractTextFromMessage(Message message)
    {
        MessagePart plainTextData;
        MessagePart htmlTextData;
        string plainText;
        string htmlText;

        plainTextData = message.Payload.Parts
            .SelectMany(x => x.Parts)
            .SelectMany(x => x.Parts)
            .SingleOrDefault(X => X.MimeType == MimeType.Text.Name);
        htmlTextData = message.Payload.Parts?
            .SelectMany(x => x.Parts)
            .SelectMany(x => x.Parts)
            .SingleOrDefault(X => X.MimeType == MimeType.Html.Name);


        if (htmlTextData is not null)
        {
            plainText = gmailPayloadService.GetTextFromHtml(htmlTextData);
            htmlText = gmailPayloadService.GetText(htmlTextData);
        }
        else
        { 
            plainText = gmailPayloadService.GetText(plainTextData);
            htmlText = string.Empty; // No HTML content available
        }

        return (plainText, htmlText);
    }

    private bool HasBothHtmlAndPlainTextMessageBody(Message message)
    {
        return message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Text.Name) is null
            && message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeType.Html.Name) is not null;
    }

    private bool HasAttachmentOnly(Message message) => messageTypeService.IsMultiPartMixed(message) && !messageTypeService.IsMultiPartMixedAndMultiPartRelated(message);
    private bool HasAttachmentAndEmbeddedImage(Message message) => messageTypeService.IsMultiPartMixedAndMultiPartRelated(message);
    private bool HasEmbeddedFileInMessageBodyOnly(Message message) => messageTypeService.IsMultiPartRelated(message);
}
