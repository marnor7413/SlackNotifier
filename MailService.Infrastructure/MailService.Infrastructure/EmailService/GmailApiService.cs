using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using MailService.Infrastructure.EmailService;
using MailService.Infrastructure.Extensions;
using System.Text;
using MailService.Infrastructure.Factories;

namespace MailService.Infrastructure.EmailServices;

public class GmailApiService : IGmailApiService
{
    private GmailService service;

    private const string AuthenticatedUser = "me";
    private const string FilterUnreadEmailsOnly = "is:unread";
    private const string InboxFolder = "INBOX";
    
    private const string HeaderEncodingValueForBase64 = "base64";
    private const string HeaderEncodingValueContentTransferEncoding = "Content-Transfer-Encoding";
    
    private const string MimeTypeText = "text/plain";
    private const string MimeTypeHtml = "text/html";
    private const string MimeTypeMultiPartMixed = "multipart/mixed";
    private const string MimeTypeMultiPartAlternative = "multipart/alternative";
    private const string MimeTypeIphonePagesFileformat = "application/x-iwork-pages-sffpages";
    private const string MimeTypeImageJpeg = "image/jpeg";

    private string base64String = string.Empty;

    public GmailApiService()
    {
        service = GmailClientFactory.CreateGmailClient();
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
        var threads = emailListResponse.Messages?.GroupBy(x => x.ThreadId) ?? Enumerable.Empty<IGrouping<string,Message>>();

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
            var email = new EmailInfo
            {
                Id = counter,
                Date = message.Payload.Headers.Single(x => x.Name == "Date").Value,
                From = message.Payload.Headers.Single(x => x.Name == "From").Value,
                Subject = message.Payload.Headers.Single(x => x.Name == "Subject").Value,
                PlainTextBody = string.Empty,
                HtmlBody = string.Empty,
            };

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
                    .SingleOrDefault(x => x.MimeType == MimeTypeText));
                email.HtmlBody = GetText(message.Payload.Parts
                    .SingleOrDefault(x => x.MimeType == MimeTypeHtml));
            }
            else if (IsMessageWithStupidIphoneAttachment(message))
            {
                email.PlainTextBody = GetText(message.Payload.Parts
                    .SingleOrDefault(x => x.MimeType == MimeTypeMultiPartAlternative)
                    .Parts.SingleOrDefault(x => x.MimeType == MimeTypeText));
                email.HtmlBody = GetText(message.Payload.Parts
                    .SingleOrDefault(x => x.MimeType == MimeTypeMultiPartAlternative)
                    .Parts.SingleOrDefault(x => x.MimeType == MimeTypeHtml));
            }
            else if (IsMultiPartAlternativeMessage(message))
            {
                email.PlainTextBody = GetText(message.Payload.Parts
                    .SingleOrDefault(x => x.MimeType == MimeTypeText));
                email.HtmlBody = GetText(message.Payload.Parts
                    .SingleOrDefault(x => x.MimeType == MimeTypeHtml));
            }
            else if (IsMultiPartMixed(message)) // text + image attachment
            {
                var textObject = message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeTypeMultiPartAlternative);
                var image = message.Payload.Parts.SingleOrDefault(x => x.MimeType == MimeTypeImageJpeg);

                var attId = image.Body.AttachmentId;
                var attachPart = await service.Users.Messages.Attachments.Get(AuthenticatedUser, message.Id, attId).ExecuteAsync();

                var jpegAttachment = new JpegAttachment
                {
                    FileName = image.Filename,
                    Description = "",
                    Data = attachPart.Data
                };
        
                email.JpegAttachments.Add(jpegAttachment);
                
                email.PlainTextBody = GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeTypeText));
                email.HtmlBody = GetText(textObject.Parts.SingleOrDefault(x => x.MimeType == MimeTypeHtml));
            }
            
            return email;
        }

        return null;
    }

    private bool IsMultiPartMixed(Message message)
    {
        return message.HasMimeType(MimeTypeMultiPartMixed) &&
            message.HasSubMimeType(MimeTypeMultiPartAlternative) && 
            message.HasSubMimeType(MimeTypeImageJpeg);
    }

    private bool IsMultiPartAlternativeMessage(Message message)
    {
        return message.HasMimeType(MimeTypeMultiPartAlternative) &&
            message.HasSubMimeType(MimeTypeText) &&
            message.HasSubMimeType(MimeTypeHtml) &&
            message.Payload.Parts.Count() == 2;
    }

    private bool IsMessageWithStupidIphoneAttachment(Message message)
    {
        return message.HasMimeType(MimeTypeMultiPartMixed) &&
            message.HasSubMimeType(MimeTypeIphonePagesFileformat);
    }

    private static bool IsAPlainMessage(Message message)
    {
        return message.HasMimeType(MimeTypeText) && 
            message.HasSubMimeType(MimeTypeText) && 
            message.HasSubMimeType(MimeTypeHtml) &&
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