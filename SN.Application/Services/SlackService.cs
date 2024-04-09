using Microsoft.Extensions.Options;
using SN.Application.Dtos;
using SN.Application.Extensions;
using SN.Application.Interfaces;
using SN.Application.Options;
using System.Net;

namespace SN.Application.Services;

public class SlackService : ISlackService
{
    private readonly ISlackApiService slackApiService;
    private readonly SecretsOptions options;

    private enum Operation
    {
        Message,
        File
    }

    public SlackService(ISlackApiService slackApiService, IOptions<List<SecretsOptions>> options)
    {
        this.slackApiService = slackApiService;
        this.options = options.Value.Single(x => x.Subject == nameof(SlackService)); ;
    }

    public async Task<bool> SendMessage(List<EmailInfo> messages)
    {
        foreach (var message in messages)
        {
            var requestBody = message.ToSlackFormattedStringContent(options.Destination);
            HttpResponseMessage sendMessageResponse;
            dynamic sendMessageResponseInfo;
            try
            {
                sendMessageResponse = await slackApiService.SendMessage(requestBody);
                sendMessageResponseInfo = await sendMessageResponse.ExtractResponseDataFromHttpResponseMessage();
            }
            catch (Exception)
            {
                return false;
            }
            LogResult(sendMessageResponse.StatusCode, Operation.Message, sendMessageResponse, message.From, string.Empty);

            foreach (var item in message.FileAttachments)
            {
                using var formData = new MultipartFormDataContent();
                using var fileContent = new ByteArrayContent(item.ToByteArray());
                formData.Add(fileContent, "file", item.FileName);
                AddParametersToFormDataObject(sendMessageResponseInfo, item, formData);

                HttpResponseMessage uploadFileResponse;
                try
                {
                    uploadFileResponse = await slackApiService.UploadFile(formData);
                    var uploadFileResponseInfo = await uploadFileResponse.ExtractResponseDataFromHttpResponseMessage();
                }
                catch (Exception)
                {
                    return false;
                }
                LogResult(uploadFileResponse.StatusCode, Operation.File, uploadFileResponse, message.From, item.FileName);
            }
        }

        return true;
    }

    private void AddParametersToFormDataObject(dynamic responseObject, FileAttachment item, MultipartFormDataContent formData)
    {
        var parameters = new[]
        {
            new KeyValuePair<string, string>("filename", item.FileName),
            new KeyValuePair<string, string>("filetype", item.fileType),
            new KeyValuePair<string, string>("channels", options.Destination),
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
