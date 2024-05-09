using Microsoft.Extensions.Options;
using SN.Application.Dtos;
using SN.Application.Extensions;
using SN.Application.Interfaces;
using SN.Application.Options;
using SN.Core.ValueObjects;
using System.Net;

namespace SN.Application.Services;

public class SlackService : ISlackService
{
    private readonly ISlackApiService slackApiService;
    private readonly ISlackBlockBuilder slackBlockBuilder;
    private readonly SecretsOptions options;

    private enum Operation
    {
        Message,
        File
    }

    public SlackService(ISlackApiService slackApiService,
        ISlackBlockBuilder slackBlockBuilder,
        IOptions<List<SecretsOptions>> options)
    {
        this.slackApiService = slackApiService;
        this.slackBlockBuilder = slackBlockBuilder;
        this.options = options.Value.Single(x => x.Subject == nameof(SlackService)); ;
    }

    public async Task<bool> SendMessage(List<EmailInfo> messages)
    {
        bool uploadFilesResult = false;
        foreach (var message in messages)
        {
            var messageThread = string.Empty;

            var blockBuilder = slackBlockBuilder
                .WithDivider()
                .WithSendDate(message.Date)
                .ToChannel(options.Destination)
                .FromSender(message.From)
                .WithSubject(message.Subject)
                .WithMessageBody(message.PlainTextBody);

            if (message.RelatedFileAttachments.Any())
            {
                (uploadFilesResult, var uploadedRelatedFiles) = await UploadFiles(message.From, message.RelatedFileAttachments);
                if (!uploadFilesResult)
                {
                    foreach (var item in uploadedRelatedFiles)
                    {
                        Console.WriteLine($"[{DateTime.Now.ToLocalTime()}] An unknown error occured when trying to upload the related file {item.Value} to slack");
                    }

                    return uploadFilesResult;
                }

                blockBuilder.WithRelatedFiles(uploadedRelatedFiles);
            }

            var requestBody = blockBuilder.Build();

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
                uploadFilesResult = false;

                return uploadFilesResult;
            }

            LogResult(sendMessageResponse.StatusCode, Operation.Message, sendMessageResponse, message.From, string.Empty);
            (uploadFilesResult, _) = await UploadFiles(message.From, message.FileAttachments, messageThread);
            if (!uploadFilesResult)
            {
                return uploadFilesResult;
            }
        }

        return uploadFilesResult;
    }

    private async Task<(bool, Dictionary<string, string>)> UploadFiles(string sender, List<FileAttachment> attachments, string messageThread = null)
    {
        var files = new Dictionary<string, string>();
        foreach (var item in attachments)
        {
            try
            {
                var getUploadUrlResponse = await slackApiService.GetUploadUrlAsync(item.fileType, item.FileName, item.ToByteArray().Length);
                var getUploadUrlResponseContent = await getUploadUrlResponse.Content.ReadAsStringAsync();
                var slackGetUploadUrlResponse = SlackGetUploadUrlResponse.FromJson(getUploadUrlResponseContent);
                if (!getUploadUrlResponse.IsSuccessStatusCode || !slackGetUploadUrlResponse.Ok)
                {
                    LogResult(HttpStatusCode.InternalServerError, Operation.File, getUploadUrlResponse, sender, item.FileName);

                    return (false, files);
                }

                var uploadFileResponse = await slackApiService.UploadFileAsync(slackGetUploadUrlResponse.UploadUrl, item.ToByteArray());
                var uploadFileResponseContent = await uploadFileResponse.Content.ReadAsStringAsync();
                await RateLimitInMilliseconds(2000);
                if (!uploadFileResponse.IsSuccessStatusCode)
                {
                    LogResult(uploadFileResponse.StatusCode, Operation.File, uploadFileResponse, sender, item.FileName);

                    return (false, files);
                }
                files.Add(item.FileName, slackGetUploadUrlResponse.FileId);
            }
            catch (Exception)
            {
                Console.WriteLine($"[{DateTime.Now.ToLocalTime()}] An unknown error occured when trying to upload the file {item.FileName} to slack");

                return (false, files);
            }
        }

        try
        {
            await RateLimitInMilliseconds(2000);
            var completeUploadResponse = await slackApiService.CompleteUploadAsync(files, messageThread);
            var completeUploadResponseContent = await completeUploadResponse.Content.ReadAsStringAsync();
            if (!completeUploadResponse.IsSuccessStatusCode)
            {
                LogCompleteUploadResult(sender, files, completeUploadResponse);

                return (false, files);
            }
            LogCompleteUploadResult(sender, files, completeUploadResponse);
        }
        catch (Exception)
        {
            foreach (var file in files)
            {
                Console.WriteLine($"[{DateTime.Now.ToLocalTime()}] An unknown error occured when trying to complete upload of the file {file.Value} to slack");
            }

            return (false, files);
        }

        return (true, files);
    }

    private async Task RateLimitInMilliseconds(int milliseconds) => await Task.Delay(milliseconds);

    private void LogCompleteUploadResult(string sender, Dictionary<string, string> files, HttpResponseMessage completeUploadResponse)
    {
        foreach (var file in files)
        {
            LogResult(completeUploadResponse.StatusCode, Operation.File, completeUploadResponse, sender, file.Key);
        }
    }

    private void LogResult(HttpStatusCode statusCode, Operation operation, HttpResponseMessage httpResponse, string sender, string filename)
    {
        var logMessage = httpResponse.IsSuccessStatusCode switch
        {
            true when operation == Operation.Message => $"[{DateTime.Now.ToLocalTime()}] Message from {sender} sent successfully to Slack.",
            false when operation == Operation.Message => $"[{DateTime.Now.ToLocalTime()}] Error occured when sending message from {sender} to Slack. Status code: {statusCode}",
            true when operation == Operation.File => $"[{DateTime.Now.ToLocalTime()}] File {filename} in message from {sender} uploaded successfully to Slack.",
            false when operation == Operation.File => $"[{DateTime.Now.ToLocalTime()}] Error uploading file {filename} in message from {sender} to Slack. Status code: {statusCode}",
            _ => "Unknown response result in communication with Slack."
        };

        Console.WriteLine(logMessage);
    }
}
