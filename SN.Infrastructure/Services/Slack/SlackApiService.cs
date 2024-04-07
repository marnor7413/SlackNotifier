using Microsoft.Extensions.Options;
using SN.Application.Interfaces;
using SN.Application.Options;

namespace SN.Infrastructure.Services.Slack;

public class SlackApiService : ISlackApiService
{
    private readonly SecretsOptions options;
    private readonly IHttpClientFactory httpClientFactory;

    public SlackApiService(IOptions<List<SecretsOptions>> options, IHttpClientFactory httpClientFactory)
    {
        this.options = options.Value.Single(x => x.Subject == nameof(SlackApiService)); ;
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

        return await client.PostAsync(SlackApiEndpoints.UploadFile, formData);
    }
}
