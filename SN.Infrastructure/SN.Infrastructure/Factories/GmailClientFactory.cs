using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Newtonsoft.Json;

namespace MailService.Infrastructure.Factories;

public static class GmailClientFactory
{
    public static GmailService CreateGmailClient()
    {
        UserCredential credential;
        string credPathToken = Path.Combine(Directory.GetCurrentDirectory(), "googleCredentials.json");
        string json = File.ReadAllText(credPathToken);

        var gsecrets = JsonConvert.DeserializeObject<GoogleClientSecrets>(json);
        credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            gsecrets.Secrets,
            new[]
            {
                GmailService.Scope.GmailModify,
                GmailService.Scope.GmailLabels,
                GmailService.Scope.MailGoogleCom
            },
            "user",
            CancellationToken.None)
        .Result;

        return new GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = nameof(GmailClientFactory)
        });
    }
}
