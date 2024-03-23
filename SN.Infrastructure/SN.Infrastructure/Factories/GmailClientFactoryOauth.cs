using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace MailService.Infrastructure.Factories;

public class GmailClientFactoryOauth : IGmailClientFactoryOauth
{
    private const string AppsettingsKey = "Appsettings:GoogleCredentialsOAuthFilename";
    private readonly string googleCredentialsFilename;

    public GmailClientFactoryOauth(IConfiguration configuration)
    {
        googleCredentialsFilename = configuration.GetSection(AppsettingsKey).Value;
    }

    public GmailService CreateGmailClient()
    {
        UserCredential credential;
        string credPathToken = Path.Combine(Directory.GetCurrentDirectory(), googleCredentialsFilename);
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
            ApplicationName = nameof(GmailClientFactoryOauth)
        });
    }
}
