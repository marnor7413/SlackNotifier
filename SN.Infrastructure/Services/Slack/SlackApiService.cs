using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using SN.Application.Interfaces;
using SN.Application.Options;
using SN.Application.Services;
using SN.Core.ValueObjects;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using static System.Net.Mime.MediaTypeNames;

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

    public async Task<HttpResponseMessage> UploadFile(MultipartFormDataContent formData)
    {
        var client = httpClientFactory.CreateClient(nameof(SlackApiService));
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.Token}");

        return await client.PostAsync(SlackApiEndpoints.UploadFile, formData);
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

    public async Task<HttpResponseMessage> CompleteUploadAsync(string fileId, string filename, string messageThread)
    {
        var jsonObject = new JObject(
            new JProperty("channel_id", $"{options.Destination}"),
            new JProperty("thread_ts", $"{messageThread}"),
            new JProperty("files",
                new JArray(
                    new JObject(
                        new JProperty("id",  $"{fileId}"),
                        new JProperty("title",  $"{filename}")
                    )
                )
            )
        );

        var requestBody = new StringContent(jsonObject.ToString(), Encoding.UTF8);
        requestBody.Headers.ContentType.MediaType = "application/json";
        
        //TODO: fixa om till JSON enligt https://api.slack.com/methods/files.completeUploadExternal
        var client = httpClientFactory.CreateClient(nameof(SlackApiService));
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.Token}");
        var response = await client.PostAsync(SlackApiEndpoints.CompleteUpload, requestBody);

        return response;
    }
}
