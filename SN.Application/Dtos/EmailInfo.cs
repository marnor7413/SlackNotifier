using System.Text.Json.Nodes;
using System.Text;
using System.Text.RegularExpressions;

namespace SN.Application.Dtos;

public record EmailInfo(int Id, string Date, string From, string Subject, string PlainTextBody, string HtmlBody)
{
    public List<FileAttachment> FileAttachments { get; init; } = new List<FileAttachment>();
    public List<FileAttachment> RelatedFileAttachments { get; init; } = new List<FileAttachment>();

    public bool Validate()
    {
        if (Id < 0) return false;
        if (string.IsNullOrWhiteSpace(From)) return false;
        if (string.IsNullOrWhiteSpace(PlainTextBody) && string.IsNullOrWhiteSpace(HtmlBody)) return false;
        if (!DateTime.TryParse(Date, out var parsatDatum)) return false;

        return true;
    }

    public EmailInfo SetMessageBody(string plain, string htmlbody)
    {
        return this with
        {
            PlainTextBody = plain,
            HtmlBody = htmlbody
        };
    }

    public StringContent ToSlackFormattedStringContent(string channel)
    {
        var jsonObject = GenerateJsonObject(channel);
        var requestBody = new StringContent(jsonObject.ToString(), Encoding.UTF8);
        requestBody.Headers.ContentType.MediaType = "application/json";

        return requestBody;
    }

    private JsonObject GenerateJsonObject(string channel)
    {
        var text = new StringBuilder();
        text.AppendLine($"*Skickat: {DateTime.Parse(Date).ToLocalTime()}*");
        text.AppendLine($"*Från: {FormatEmailLinkInFromText(From)}*");
        text.AppendLine($"*Ämne: {Subject}*");
        text.AppendLine(PlainTextBody);

        var json = new JsonObject
        {
            { "channel", channel },
            { "text", text.ToString() }
        };

        return json;
    }

    private string FormatEmailLinkInFromText(string text)
    {
        if (text.Contains('@') && text.Contains('<') && text.Contains('>'))
        {
            var emailPattern = @"<(.*?)>";
            var regExMatch = Regex.Match(text, emailPattern);
            if (regExMatch.Success)
            {
                var emailText = regExMatch.Groups[1].Value;
                string replacedText = Regex.Replace(text, emailPattern, $"<mailto:{emailText}|{emailText}>");

                return replacedText;
            }
        }

        return text;
    }
}