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
    private ISlackBlockBuilder blockBuilder;
    private readonly int maxAmountOfCharacters = 3000;

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
                        Console.WriteLine($"[{DateTime.Now.ToLocalTime()}] An unknown error occured when trying to upload the related file {item.Value} to slack");
                    }

                    return uploadFilesResult;
                }
            }

            var requestBodiesToSend = new List<StringContent>();
            for (int i = 0; i < splitMessage.Count; i++)
            {
                if (i > 0)
                {
                    slackBlockBuilder.Clear();
                    blockBuilder = slackBlockBuilder.WithMessageBody(splitMessage[i]);
                }
                else
                {
                    blockBuilder = slackBlockBuilder
                        .WithHeaderTitle("Mail vidarebefodrat från orgrytetorp@gmail.com:")
                        .WithSendDate(message.Date)
                        .FromSender(message.From)
                        .WithSubject(message.Subject)
                        .WithMessageBody(splitMessage[i]);
                }

                blockBuilder.ToChannel(options.Destination);

                if (uploadedRelatedFiles.Any())
                {
                    blockBuilder.WithRelatedFiles(uploadedRelatedFiles);
                }
                requestBodiesToSend.Add(blockBuilder.Build());
            }

            HttpResponseMessage sendMessageResponse = null;
            dynamic sendMessageResponseInfo;
            try
            {
                foreach (var requestBody in requestBodiesToSend)
                {
                    await Task.Delay(1000);
                    sendMessageResponse = await slackApiService.SendMessage(requestBody);
                    sendMessageResponseInfo = await sendMessageResponse.ExtractResponseDataFromHttpResponseMessage();
                    messageThread = (string)sendMessageResponseInfo.ts;
                }
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
