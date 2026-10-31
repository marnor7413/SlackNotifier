using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SN.Application.Dtos;
using SN.Application.Extensions;
using SN.Application.Interfaces;
using SN.Application.Metrics;
using SN.Application.Options;
using SN.Core.ValueObjects;
using System.Net;

namespace SN.Application.Services;

public class SlackService : ISlackService
{
    private readonly ISlackApiService slackApiService;
    private readonly ISlackBlockBuilder slackBlockBuilder;
    private readonly ILogger<SlackService> logger;
    private readonly SlackNotifierMetrics metrics;
    private readonly SlackSecretsOptions options;
    private readonly int maxAmountOfCharacters = 3000;

    private enum Operation
    {
        Message,
        File
    }

    public SlackService(ISlackApiService slackApiService,
        ISlackBlockBuilder slackBlockBuilder,
        IOptions<SlackSecretsOptions> options,
        ILogger<SlackService> logger,
        SlackNotifierMetrics metrics)
    {
        this.slackApiService = slackApiService;
        this.slackBlockBuilder = slackBlockBuilder;
        this.logger = logger;
        this.metrics = metrics;
        this.options = options.Value;
    }

    public async Task<bool> SendMessage(List<EmailInfo> messages)
    {
        bool uploadFilesResult = false;
        foreach (var message in messages)
        {
            var messageThread = string.Empty;
            var splitMessage = new List<string>();
            if (message.PlainTextBody.ToCharArray().Length > maxAmountOfCharacters)
            {
                splitMessage = SplitTextIntoChunks(message.PlainTextBody, maxAmountOfCharacters);
            }
            else
            {
                splitMessage.Add(message.PlainTextBody);
            }

            var uploadedRelatedFiles = new Dictionary<string, string>();
            if (message.RelatedFileAttachments.Any())
            {
                (uploadFilesResult, uploadedRelatedFiles) = await UploadFiles(message.From, message.RelatedFileAttachments);
                if (!uploadFilesResult)
                {
                    foreach (var item in uploadedRelatedFiles)
                    {
                        logger.LogError($"---> An error occured when trying to upload the related file {item.Value} to slack for message from {message.From}");
                    }

                    return uploadFilesResult;
                }
            }

            var requestBodiesToSend = new List<StringContent>();
            for (int i = 0; i < splitMessage.Count; i++)
            {
                if (i > 0)
                {
                    slackBlockBuilder.WithMessageBody(splitMessage[i]);
                }
                else
                {
                    slackBlockBuilder
                        .WithHeaderTitle("_Nytt mail mottaget på orgrytetorp@gmail.com_")
                        .WithSendDate(message.Date)
                        .FromSender(message.From)
                        .WithSubject(message.Subject)
                        .WithMessageBody(splitMessage[i]);
                }

                slackBlockBuilder.ToChannel(options.Destination);

                if (uploadedRelatedFiles.Any())
                {
                    slackBlockBuilder.WithRelatedFiles(uploadedRelatedFiles);
                }
                requestBodiesToSend.Add(slackBlockBuilder.Build());
                slackBlockBuilder.Clear();
            }

            HttpResponseMessage sendMessageResponse = null;
            dynamic sendMessageResponseInfo;
            var allPartsAccepted = true;

            try
            {
                foreach (var requestBody in requestBodiesToSend)
                {
                    await Task.Delay(1000);
                    sendMessageResponse = await slackApiService.SendMessage(requestBody);
                    if (!sendMessageResponse.IsSuccessStatusCode)
                    {
                        metrics.SlackRequestFailed(sendMessageResponse.StatusCode);
                        allPartsAccepted = false;
                    }

                    sendMessageResponseInfo = await sendMessageResponse.ExtractResponseDataFromHttpResponseMessage();
                    messageThread = (string)sendMessageResponseInfo.ts;
                    if (sendMessageResponse.IsSuccessStatusCode && (bool?)sendMessageResponseInfo.ok != true)
                    {
                        metrics.SlackRequestRejected((string)sendMessageResponseInfo.error);
                        allPartsAccepted = false;
                    }
                }
            }
            catch (Exception ex)
            {
                metrics.SlackRequestThrew(ex);
                logger.LogError(ex, "---> An unknown error occured when sending a message to Slack.");
                uploadFilesResult = false;

                return uploadFilesResult;
            }

            if (allPartsAccepted)
            {
                metrics.SlackEmailPosted();
            }

            LogResult(sendMessageResponse.StatusCode, Operation.Message, sendMessageResponse, message.From, string.Empty);
            (uploadFilesResult, _) = await UploadFiles(message.From, message.FileAttachments, messageThread);
            if (!uploadFilesResult)
            {
                return uploadFilesResult;
            }
        }

        messages.Clear();
        return uploadFilesResult;
    }

    private List<string> SplitTextIntoChunks(string text, int maxChunkLength)
    {
        const string space = " ";
        const int lineBreakCharacterCount = 2;
        const int spaceCount = 1;
        const string lineBreakCharacters = "\r\n";
        List<string> chunks = new List<string>();
        string[] lines = text.Split(new string[] { lineBreakCharacters }, StringSplitOptions.None);

        string currentChunk = string.Empty;
        foreach (string line in lines)
        {
            if (line.StartsWith(">"))
            {
                if (currentChunk.Length + line.Length + lineBreakCharacterCount > maxChunkLength)
                {
                    chunks.Add(currentChunk);
                    currentChunk = string.Empty;
                }
                currentChunk += line + lineBreakCharacters;
            }
            else
            {
                string[] words = line.Split(' ');
                foreach (string word in words)
                {
                    if (currentChunk.Length + word.Length + spaceCount > maxChunkLength)
                    {
                        chunks.Add(currentChunk.TrimEnd());
                        currentChunk = string.Empty;
                    }

                    currentChunk += (currentChunk == string.Empty ? string.Empty : space) + word;
                }
                currentChunk += lineBreakCharacters;
            }
        }

        if (currentChunk != string.Empty)
        {
            chunks.Add(currentChunk.TrimEnd()); 
        }

        return chunks;
    }

    private async Task<(bool, Dictionary<string, string>)> UploadFiles(string sender, List<FileAttachment> attachments, string messageThread = null)
    {
        var files = new Dictionary<string, string>();
        long uploadedBytes = 0;
        foreach (var item in attachments)
        {
            try
            {
                var fileBytes = item.ToByteArray();
                var getUploadUrlResponse = await slackApiService.GetUploadUrlAsync(item.FileType, item.FileName, item.ToByteArray().Length);
                var getUploadUrlResponseContent = await getUploadUrlResponse.Content.ReadAsStringAsync();
                var slackGetUploadUrlResponse = SlackGetUploadUrlResponse.FromJson(getUploadUrlResponseContent);
                if (!getUploadUrlResponse.IsSuccessStatusCode || !slackGetUploadUrlResponse.Ok)
                {
                    RecordFailedSlackResponse(getUploadUrlResponse, slackGetUploadUrlResponse.Error);
                    LogResult(HttpStatusCode.InternalServerError, Operation.File, getUploadUrlResponse, sender, item.FileName);

                    return (false, files);
                }

                var uploadFileResponse = await slackApiService.UploadFileAsync(slackGetUploadUrlResponse.UploadUrl, item.ToByteArray());
                var uploadFileResponseContent = await uploadFileResponse.Content.ReadAsStringAsync();
                await RateLimitInMilliseconds(2000);
                if (!uploadFileResponse.IsSuccessStatusCode)
                {
                    metrics.SlackRequestFailed(uploadFileResponse.StatusCode);
                    LogResult(uploadFileResponse.StatusCode, Operation.File, uploadFileResponse, sender, item.FileName);

                    return (false, files);
                }
                if(!files.ContainsKey(item.FileName))
                {
                    files.Add(item.FileName, slackGetUploadUrlResponse.FileId);
                    uploadedBytes += fileBytes.Length; // metrics
                }
            }
            catch (Exception ex)
            {
                metrics.SlackRequestThrew(ex); // metrics
                logger.LogError(ex, $"---> An unknown error occured when trying to upload the file {item.FileName} to slack.");

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
                if (files.Count > 0)
                {
                    metrics.SlackRequestFailed(completeUploadResponse.StatusCode);
                }
                LogCompleteUploadResult(sender, files, completeUploadResponse);

                return (false, files);
            }

            if (files.Count > 0)
            {
                RecordCompleteUploadResult(completeUploadResponseContent, files.Count, uploadedBytes);
            }
            LogCompleteUploadResult(sender, files, completeUploadResponse);
        }
        catch (Exception ex)
        {
            metrics.SlackRequestThrew(ex); // metrics
            foreach (var file in files)
            {
                logger.LogError(null, null, $"---> An unknown error occured when trying to complete upload of the file {file.Value} to slack for message from {sender}");
            }

            return (false, files);
        }

        return (true, files);
    }

    private void RecordFailedSlackResponse(HttpResponseMessage response, string slackErrorCode)
    {
        if (!response.IsSuccessStatusCode)
        {
            metrics.SlackRequestFailed(response.StatusCode);
        }
        else
        {
            metrics.SlackRequestRejected(slackErrorCode);
        }
    }

    private void RecordCompleteUploadResult(string responseContent, int fileCount, long totalBytes) // metrics
    {
        try
        {
            dynamic responseInfo = JsonConvert.DeserializeObject<dynamic>(responseContent);
            if ((bool?)responseInfo?.ok == true)
            {
                metrics.SlackAttachmentsUploaded(fileCount, totalBytes);
            }
            else
            {
                metrics.SlackRequestRejected((string)responseInfo?.error);
            }
        }
        catch (Exception exception)
        {
            metrics.SlackRequestThrew(exception);
        }
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
            true when operation == Operation.Message => $"---> Message from {sender} sent successfully to Slack.",
            false when operation == Operation.Message => $"---> Error occured when sending message from {sender} to Slack. Status code: {statusCode}",
            true when operation == Operation.File => $"---> File {filename} in message from {sender} uploaded successfully to Slack.",
            false when operation == Operation.File => $"---> Error uploading file {filename} in message from {sender} to Slack. Status code: {statusCode}",
            _ => "---> Unknown response result in communication with Slack."
        };

        logger.LogInformation(logMessage);
    }
}
