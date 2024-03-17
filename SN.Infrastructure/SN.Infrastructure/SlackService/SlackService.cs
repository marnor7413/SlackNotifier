using MailService.ConsoleApp.Configuration;
using MailService.Infrastructure.EmailService;
using Microsoft.Extensions.Options;
using SlackAPI;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace MailService.Infrastructure.SlackServices;

public class SlackService : ISlackService
{
    private readonly SecretsOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public SlackService(IOptions<List<SecretsOptions>> options, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value.Single(x => x.Subject == nameof(SlackService));
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendMessage(List<EmailInfo> messages)
    {
        var client = _httpClientFactory.CreateClient(nameof(SlackService));
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer { _options.Token }");

        //implementera detta istället: https://api.slack.com/methods/chat.postMessage

        foreach (var message in messages)
        {
            var text = new StringBuilder();
            text.AppendLine($"*Från: {FormatEmailLinkInFromText(message.From)}*");
            text.AppendLine($"*Ämne: {message.Subject}*");
            text.AppendLine(message.PlainTextBody);

            var hej = new JsonObject
            {
                { "channel", _options.Destination },
                { "text", text.ToString() }
            };
            var requestBody = new StringContent(hej.ToString(), Encoding.UTF8);
            requestBody.Headers.ContentType.MediaType = "application/json";
            var response = await client.PostAsync("/api/chat.postMessage", requestBody);


            var attachments = new List<Attachment>();
            attachments.AddRange(GetJpegAttachments(message.JpegAttachments));

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Meddelande ej skickat till Slack");
            }

            //Upload the file to Slack
            //foreach (var item in message.JpegAttachments)
            //{
            //    await slackClient.UploadFileAsync(
            //            item.ToByteArray(),
            //            item.FileName,
            //            new[] { "CJJTR812L" },
            //            item.FileName,
            //            item.Description,
            //            fileType: "image/jpeg");
            //}
        }
    }

    private IEnumerable<Attachment> GetJpegAttachments(List<JpegAttachment> jpegAttachments)
    {
        var result = new List<Attachment>();
        foreach (var item in jpegAttachments)
        {
            result.Add(new Attachment()
            {
                title = item.FileName,
                text = item.Description,
                image_url = $"data:image/jpeg;base64,{item.Data}"
            });
        }

        return result;
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
