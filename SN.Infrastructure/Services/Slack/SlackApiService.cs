﻿using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using SN.Application.Interfaces;
using SN.Application.Options;
using SN.Application.Services;
using System.Text;

namespace SN.Infrastructure.Services.Slack;

public class SlackApiService : ISlackApiService
{
    private readonly SecretsOptions options;
    private readonly IHttpClientFactory httpClientFactory;

    public SlackApiService(IOptions<List<SecretsOptions>> options, IHttpClientFactory httpClientFactory)
    {
        this.options = options.Value.Single(x => x.Subject == nameof(SlackService)); ;
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<HttpResponseMessage> SendMessage(StringContent requestBody)
    {
        var client = httpClientFactory.CreateClient(nameof(SlackApiService));
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.Token}");

        return await client.PostAsync(SlackApiEndpoints.PostMessage, requestBody);
    }

    public async Task<HttpResponseMessage> GetUploadUrlAsync(string fileType, string filename, long filesize)
    {
        var requestUrl = $"{SlackApiEndpoints.GetUploadUrl}" +
                         $"?filename={filename}&" +
                         $"length={filesize}";
        var client = httpClientFactory.CreateClient(nameof(SlackApiService));
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.Token}");
        var response = await client.GetAsync(requestUrl);
               
        return response;
    }

    public async Task<HttpResponseMessage> UploadFileAsync(string uploadUrl, byte[] fileBytes)
    {
        var client = httpClientFactory.CreateClient(nameof(SlackApiService));
        using var content = new ByteArrayContent(fileBytes);
        var response = await client.PostAsync(uploadUrl, content);

        return response;
    }

    public async Task<HttpResponseMessage> CompleteUploadAsync(Dictionary<string,string> files, string messageThread = null)
    {
        var jsonObject = new JObject(
            new JProperty("files",
                new JArray(files.Select(f =>
                {
                    return new JObject(
                        new JProperty("id", $"{f.Value}"), 
                        new JProperty("title", $"{f.Key}"));
                }))
            ));

        if (!string.IsNullOrWhiteSpace(messageThread))
        {
            jsonObject.Add(new JProperty("channel_id", $"{options.Destination}"));
            jsonObject.Add(new JProperty("initial_comment", files.Count > 1 ? "Bifogade filer" : "Bifogad fil"));
            jsonObject.Add(new JProperty("thread_ts", $"{messageThread}"));
        }

        var requestBody = new StringContent(jsonObject.ToString(), Encoding.UTF8);
        requestBody.Headers.ContentType.MediaType = "application/json";
        
        var client = httpClientFactory.CreateClient(nameof(SlackApiService));
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.Token}");
        var response = await client.PostAsync(SlackApiEndpoints.CompleteUpload, requestBody);

        return response;
    }
}
