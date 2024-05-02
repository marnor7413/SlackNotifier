using Microsoft.Extensions.Options;
using SN.Application.Dtos;
using SN.Application.Extensions;
using SN.Application.Interfaces;
using SN.Application.Options;
using SN.Core.ValueObjects;
using System.Net;
using static Google.Apis.Requests.BatchRequest;

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
        bool uploadFilesResult = false;
        foreach (var message in messages)
        {
            var messageThread = string.Empty;
            var requestBody = message.ToSlackFormattedStringContent(options.Destination);
            HttpResponseMessage sendMessageResponse;
            dynamic sendMessageResponseInfo;
            try
            {
                sendMessageResponse = await slackApiService.SendMessage(requestBody);
                sendMessageResponseInfo = await sendMessageResponse.ExtractResponseDataFromHttpResponseMessage();
                messageThread = (string)sendMessageResponseInfo.ts;
            }
            catch (Exception)
            {
                return uploadFilesResult;
            }
            LogResult(sendMessageResponse.StatusCode, Operation.Message, sendMessageResponse, message.From, string.Empty);
            uploadFilesResult = await UploadFiles(message, messageThread);

            if (!uploadFilesResult)
            {
                return uploadFilesResult;
            }
        }

        return uploadFilesResult;
    }

    private async Task<bool> UploadFiles(EmailInfo message, string messageThread)
    {
        foreach (var item in message.FileAttachments)
        {
            try
            {
                var getUploadUrlResponse = await slackApiService.GetUploadUrlAsync(item.fileType, item.FileName, item.ToByteArray().Length);
                var responseBody = await getUploadUrlResponse.Content.ReadAsStringAsync();
                var slackGetUploadUrlResponse = SlackGetUploadUrlResponse.FromJson(responseBody);
                if (!getUploadUrlResponse.IsSuccessStatusCode || !slackGetUploadUrlResponse.Ok)
                {
                    LogResult(HttpStatusCode.InternalServerError, Operation.File, getUploadUrlResponse, message.From, item.FileName);

                    return false;
                }

                var uploadResponse = await slackApiService.UploadFileAsync(slackGetUploadUrlResponse.UploadUrl, item.ToByteArray());
                var testBody = await uploadResponse.Content.ReadAsStringAsync();

                if (!uploadResponse.IsSuccessStatusCode)
                {
                    LogResult(uploadResponse.StatusCode, Operation.File, uploadResponse, message.From, item.FileName);

                    return false;
                }

                var completeUploadResponse = await slackApiService.CompleteUploadAsync(slackGetUploadUrlResponse.FileId, item.FileName, messageThread);
                var hej = await completeUploadResponse.Content.ReadAsStringAsync();
                if (!completeUploadResponse.IsSuccessStatusCode)
                {
                    LogResult(completeUploadResponse.StatusCode, Operation.File, completeUploadResponse, message.From, item.FileName);

                    return false;
                }
                LogResult(completeUploadResponse.StatusCode, Operation.File, completeUploadResponse, message.From, item.FileName);
            }
            catch (Exception)
            {
                Console.WriteLine("An unknown error occured when trying to upload a file to slack");

                return false;
            }
        }

        return true;
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
