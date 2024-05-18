using System.Text;
using System.Text.RegularExpressions;
using SN.Application.Interfaces;
using Newtonsoft.Json;

namespace SN.Application.Builders;

public class SlackBlockBuilder : ISlackBlockBuilder
{
    private readonly string timeSentIconUri = "https://cdn-icons-png.freepik.com/256/10140/10140939.png?ga=GA1.1.1928895700.1714931280";
    private readonly string nameIconuri = "https://cdn-icons-png.freepik.com/256/12373/12373258.png?ga=GA1.1.1928895700.1714931280";
    private readonly string emailIconUri = "https://cdn-icons-png.freepik.com/256/10140/10140847.png?ga=GA1.1.1928895700.1714931280";
    
    private Blocks root = new Blocks();
    private ContextBlock sentAtDate = null;
    private ContextBlock senderName = null;
    private ContextBlock senderEmail = null;
    private SectionBlock subject = null;

    private string messageBody = string.Empty;
    private Dictionary<string,string> relatedFiles = new();

    public string CurrentSenderName 
    { 
        get
        {
            if (senderName is null ) return string.Empty;

            var text = (senderName
                .elements
                .Single(e => e.GetType() == typeof(MarkdownBlock)) as MarkdownBlock)
                .text;

            return text;
        }
    }

    public string CurrentSenderEmail
    {
        get
        {
            if (senderEmail is null) return string.Empty;

            var text = (senderEmail
                .elements
                .Single(e => e.GetType() == typeof(MarkdownBlock)) as MarkdownBlock)
                .text;

            return text;
        }
    }

    public void Clear()
    {
        root = new Blocks();
        sentAtDate = null;
        senderName = null;
        senderEmail = null;
        subject = null;
        messageBody = string.Empty;
        relatedFiles = new Dictionary<string, string>();
    }

    public ISlackBlockBuilder ToChannel(string channel)
    {
        root.channel = channel;

        return this;
    }

    public ISlackBlockBuilder WithHeaderTitle(string header)
    {
        root.blocks.Add(new SectionBlock(header));

        return this;
    }

    public ISlackBlockBuilder WithDivider()
    {
        root.blocks.Add(new DividerBlock());

        return this;
    }

    public ISlackBlockBuilder WithSendDate(string date)
    {
        sentAtDate = new ContextBlock(timeSentIconUri, $"*Skickat:* {DateTime.Parse(date).ToLocalTime()}");

        return this;
    }
    
    public ISlackBlockBuilder FromSender(string sender)
    {
        var formattedText = FormatEmailLinkInFromText(sender);
        var email = ExtractEmail(formattedText).Trim();
        var name = formattedText
            .Replace(email, string.Empty)
            .Replace("\"", string.Empty)
            .Replace("\\", string.Empty)
            .Trim();

        senderName = new ContextBlock(nameIconuri, $"*Från:* {name}");
        senderEmail = new ContextBlock(emailIconUri, $"*Email:* {email}");

        return this;
    }
    
    public ISlackBlockBuilder WithSubject(string subject)
    {
        this.subject = new SectionBlock($":slack: _*Ämne: {subject}*_");

        return this;
    }

    public ISlackBlockBuilder WithMessageBody(string messageBody)
    {
        string noAdditionalLineBreaks = Regex.Replace(messageBody, @"(\r\n){3,}", "\r\n\r\n");
        this.messageBody = noAdditionalLineBreaks;

        return this;
    }

    public ISlackBlockBuilder WithRelatedFile(string fileName, string fileId)
    {
        relatedFiles.Add(fileName, fileId);

        return this;
    }

    public ISlackBlockBuilder WithRelatedFiles(Dictionary<string, string> fileNames)
    {
        foreach (var file in fileNames)
        {
            relatedFiles.Add(file.Key, file.Value);
        }

        return this;
    }

    public StringContent Build()
    {
        var jsonObject = GenerateJsonString();
        var requestBody = new StringContent(jsonObject, Encoding.UTF8);
        requestBody.Headers.ContentType.MediaType = "application/json";

        return requestBody;
    }

    private string GenerateJsonString()
    {
        if (sentAtDate is not null) root.blocks.Add(sentAtDate);
        if (senderName is not null) root.blocks.Add(senderName);
        if (senderEmail is not null) root.blocks.Add(senderEmail);
        if (subject is not null) 
        {
            root.blocks.Add(new DividerBlock());
            root.blocks.Add(subject);
        }

        var containsEmbeddedImage = relatedFiles.Any();
        if (containsEmbeddedImage)
        {
            var messageChunks = SeparateMessageBodyTextAndImageTags();
            foreach (var item in messageChunks)
            {
                if (relatedFiles.TryGetValue(item, out string fileId))
                {
                    root.blocks.Add(new ImageFileIdBlock("file", fileId, "file"));
                }
                else
                {
                    root.blocks.Add(new SectionBlock(item));
                }
            }
        }
        else
        {
            root.blocks.Add(new SectionBlock(messageBody));
        }

        root.blocks.Add(new DividerBlock());
        var stringJsonObject = JsonConvert.SerializeObject(root);

        return stringJsonObject;
    }

    private List<string> SeparateMessageBodyTextAndImageTags()
    {
        var currentPosition = 0;
        var separatedText = new List<string>();

        foreach (var fileName in relatedFiles.Select(x => x.Key))
        {
            var imageTag = $"[image: {fileName}]";

            var startIndex = messageBody.IndexOf(imageTag, currentPosition);
            if (startIndex != -1)
            {
                if (startIndex > currentPosition)
                {
                    separatedText.Add(messageBody.Substring(currentPosition, startIndex - currentPosition));
                }
                separatedText.Add(fileName);
                currentPosition = startIndex + imageTag.Length;
            }
        }

        if (currentPosition < messageBody.Length)
        {
            separatedText.Add(messageBody.Substring(currentPosition));
        }

        return separatedText;
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
        else if (text.Contains('@'))
        {
            text = $"{text.Split('@')[0]} <mailto:{text}|{text}>";
        }

        return text;
    }

    private string ExtractEmail(string formattedText)
    {
        var emailPattern = @"<mailto:(.*?)\|.*?>";
        var regExMatch = Regex.Match(formattedText, emailPattern);
        if (regExMatch.Success)
        {
            var emailLink = regExMatch.Value;
            return emailLink;
        }
        else
        {
            return string.Empty;
        }
    }

    private class Blocks
    {
        public string channel;
        public readonly List<object> blocks = new List<object>();

        public Blocks()
        {
        }
    }

    private class HeaderBlock
    {
        public readonly string type = "header";
        public readonly PlainTextBlock text;
        
        public HeaderBlock(string headerText)
        {
            text = new PlainTextBlock(headerText);
        }
    }

    private class ContextBlock
    {
        public readonly string type = "context";
        public readonly List<object> elements = new List<object>();

        public ContextBlock(string imageUrl, string markdownText)
        {
            elements.Add(new ImageUrlBlock(imageUrl));
            elements.Add(new MarkdownBlock(markdownText));
        }

    }
    
    private class SectionBlock
    {
        public readonly string type = "section";
        public readonly MarkdownBlock text;

        public SectionBlock(string markdownText)
        {
            text = new MarkdownBlock(markdownText);
        }
    }

    private class MarkdownBlock
    {
        public readonly string type = "mrkdwn";
        public readonly string text;

        public MarkdownBlock(string markdownText)
        {
            text = markdownText;
        }
    }

    private class ImageUrlBlock
    {
        public readonly string type = "image";
        public readonly string image_url;
        public readonly string alt_text = "bild";

        public ImageUrlBlock(string imageUrl)
        {
            image_url = imageUrl;
        }
    }

    private class ImageFileIdBlock
    {
        public readonly string type = "image";
        public readonly PlainTextBlock title;
        public readonly SlackFileBlock slack_file;
        public readonly string alt_text;

        public ImageFileIdBlock(string title, string fileId, string altText)
        {
            this.title = new PlainTextBlock(title);
            slack_file = new SlackFileBlock(fileId);
            alt_text = altText;
        }
    }

    private class SlackFileBlock
    {
        public readonly string id;

        public SlackFileBlock(string fileId)
        {
            id = fileId;
        }
    }

    private class PlainTextBlock
    {
        public readonly string type = "plain_text";
        public readonly string text;

        public PlainTextBlock(string text)
        {
            this.text = text;
        }
    }

    private class TextBlock
    {
        public readonly string type = "text";
        public readonly string text;

        public TextBlock(string text)
        {
            this.text = text;
        }
    }

    private class DividerBlock
    {
        public readonly string type = "divider";

    }
}
