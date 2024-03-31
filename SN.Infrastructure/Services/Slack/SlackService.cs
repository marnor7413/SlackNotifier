using MailService.ConsoleApp.Configuration;
using MailService.Infrastructure.EmailService;
using MailService.Infrastructure.Extensions;
using MailService.Infrastructure.SlackServices;
using Microsoft.Extensions.Options;

namespace SN.Infrastructure.Services.Slack;

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
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.Token}");

        foreach (var message in messages)
        {
            var requestBody = message.ToSlackFormattedStringContent(_options.Destination);
            var sendMessageResponse = await client.PostAsync(SlackApiEndpoints.PostMessage, requestBody);
            var sendMessageResponseInfo = await sendMessageResponse.ExtractResponseDataFromHttpResponseMessage();
            var sendMessageResult = sendMessageResponse.IsSuccessStatusCode switch
            {
                true => "Message sent successfully.",
                false => $"Error occured when sending message. Status code: {sendMessageResponse.StatusCode}"
            };
            Console.WriteLine(sendMessageResult);

            foreach (var item in message.FileAttachments)
            {
                using var formData = new MultipartFormDataContent();
                using var fileContent = new ByteArrayContent(item.ToByteArray());
                formData.Add(fileContent, "file", item.FileName);

                foreach (var parameter in CreateDefaultParametersForFileUpload(sendMessageResponseInfo, item))
                {
                    formData.Add(new StringContent(parameter.Value), parameter.Key);
                }

                var uploadFileResponse = await client.PostAsync(SlackApiEndpoints.UploadFile, formData);
                var uploadFileResponseInfo = await uploadFileResponse.ExtractResponseDataFromHttpResponseMessage();
                var uploadResult = uploadFileResponse.IsSuccessStatusCode switch
                {
                    true => $"File {item.FileName} uploaded successfully.",
                    false => $"Error uploading file. Status code: {uploadFileResponse.StatusCode}"
                };
                Console.WriteLine(uploadResult);
            }
        }
    }

    private KeyValuePair<string, string>[] CreateDefaultParametersForFileUpload(dynamic responseObject, FileAttachment item)
    {
        return new[]
        {
            new KeyValuePair<string, string>("filename", item.FileName),
            new KeyValuePair<string, string>("filetype", item.fileType),
            new KeyValuePair<string, string>("channels", _options.Destination),
            new KeyValuePair<string, string>("initial_comment", item.FileName),
            new KeyValuePair<string, string>("title", "Bifogad fil"),
            new KeyValuePair<string, string>("thread_ts", (string)responseObject.ts)
        };
    }
}
