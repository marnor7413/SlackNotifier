using Google.Apis.Gmail.v1.Data;
using HtmlAgilityPack;
using SN.Application.Dtos;
using SN.Application.Interfaces;
using SN.Core.ValueObjects;
using System.Net;
using System.Text;

namespace SN.Application.Services;

public class GmailPayloadService : IGmailPayloadService
{
    private readonly IGmailApiService gmailApiService;

    public GmailPayloadService(IGmailApiService gmailApiService)
    {
        this.gmailApiService = gmailApiService;
    }

    public string GetTextFromHtml(MessagePart payload)
    {
        if (EmailBodyTextExists(payload))
        {
            try
            {
                var base64String = FileAttachment.Base64UrlSafeStringToBase64Standard(payload.Body.Data);
                byte[] data = Convert.FromBase64String(base64String);
                var htmlContent = Encoding.UTF8.GetString(data);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);
                
                var plainText = ExtractPlainTextFromHtml(doc.DocumentNode);
                var decodedText = WebUtility.HtmlDecode(plainText); // to correct &nbsp 
                
                return decodedText;
            }
            catch (Exception)
            {
                // no implementation
            }

        }

        return string.Empty;
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
    
    public IEnumerable<MessagePart> GetAttachmentData(IList<MessagePart> parts)
    {
        if(parts is not IList<MessagePart>)
        {
            return Enumerable.Empty<MessagePart>();
        }

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
            var attachPart = await gmailApiService.DownloadAttachment(messageId, attachmentId);
            var attachment = new FileAttachment(item.Filename, fileType.Name, "", attachPart.Data);
            if (attachment.Validate())
            {
                emailAttachments.Add(attachment);
            }
        }

        return emailAttachments;
    }

    private static bool EmailBodyTextExists(MessagePart payload)
    {
        return payload is not null 
            && payload.Body is not null 
            && !string.IsNullOrWhiteSpace(payload.Body.Data);
    }

    private static string ExtractPlainTextFromHtml(HtmlNode node)
    {
        if (node.NodeType == HtmlNodeType.Text)
        {
            return node.InnerText;
        }

        if (node.NodeType == HtmlNodeType.Element && node.Name == "br")
        {
            return Environment.NewLine;
        }

        string result = "";
        foreach (var child in node.ChildNodes)
        {
            result += ExtractPlainTextFromHtml(child);
        }

        return result;
    }
}
