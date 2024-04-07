using MailService.Infrastructure.Extensions;
using Microsoft.Extensions.Options;
using SN.Application.Dtos;
using SN.Application.Interfaces;
using SN.Application.Options;
using System.Net;

namespace SN.Infrastructure.Services.Slack;

public class SlackService : ISlackService
{
    private readonly SecretsOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    private enum Operation 
    {
        Message,
        File
    }

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
            LogResult(sendMessageResponse.StatusCode, Operation.Message, sendMessageResponse, message.From, string.Empty);

            foreach (var item in message.FileAttachments)
            {
                using var formData = new MultipartFormDataContent();
                using var fileContent = new ByteArrayContent(item.ToByteArray());
                formData.Add(fileContent, "file", item.FileName);
                AddParametersToFormDataObject(sendMessageResponseInfo, item, formData);

                var uploadFileResponse = await client.PostAsync(SlackApiEndpoints.UploadFile, formData);
                var uploadFileResponseInfo = await uploadFileResponse.ExtractResponseDataFromHttpResponseMessage();
                LogResult(uploadFileResponse.StatusCode, Operation.File, uploadFileResponse, message.From, item.FileName);
            }
        }
    }

    private void AddParametersToFormDataObject(dynamic responseObject, FileAttachment item, MultipartFormDataContent formData)
    {
        var parameters = new[]
        {
            new KeyValuePair<string, string>("filename", item.FileName),
            new KeyValuePair<string, string>("filetype", item.fileType),
            new KeyValuePair<string, string>("channels", _options.Destination),
            new KeyValuePair<string, string>("initial_comment", item.FileName),
            new KeyValuePair<string, string>("title", "Bifogad fil"),
            new KeyValuePair<string, string>("thread_ts", (string)responseObject.ts)
        };

        foreach (var parameter in parameters)
        {
            formData.Add(new StringContent(parameter.Value), parameter.Key);
        }
    }

    private void LogResult(HttpStatusCode statusCode, Operation operation, HttpResponseMessage httpResponse, string from, string filename)
    {
        var logMessage = httpResponse.IsSuccessStatusCode switch
        {
            true when operation == Operation.Message => $"[{DateTime.Now.ToLocalTime()}] Message from {from} sent successfully to Slack.",
            false when operation == Operation.Message => $"[{DateTime.Now.ToLocalTime()}] Error occured when sending message from {from} to Slack. Status code: {statusCode}",
            true when operation == Operation.File => $"[{DateTime.Now.ToLocalTime()}] File {filename} in message from {from} uploaded successfully to Slack.",
            false when operation == Operation.File => $"[{DateTime.Now.ToLocalTime()}] Error uploading file {filename} in message from {from} to Slack. Status code: {statusCode}",
            _ => "Unknown response result in communication with Slack."
        };

        Console.WriteLine(logMessage);
    }
}
