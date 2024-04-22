using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SN.Application.Interfaces;

namespace MailService.Infrastructure.Factories;

public class GmailServiceFactory : IGmailServiceFactory
{
    public const string AuthenticatedUser = "me";
    private readonly string storedCredentialsFilename = "token.json";
    private readonly string AppsettingsKey = "Appsettings:GoogleCredentialsOAuthFilename";
    private readonly string googleCredentialsFilename;
    
    private readonly IGoogleAuthService googleAuthService;
    private readonly IIOService iOService;

    public GmailServiceFactory(IConfiguration configuration, 
        IGoogleAuthService googleAuthService,
        IIOService iOService)
    {
        googleCredentialsFilename = configuration.GetSection(AppsettingsKey).Value;
        this.googleAuthService = googleAuthService;
        this.iOService = iOService;
    }

    public async Task<GmailService> GetService()
    {
        var credentials = iOService.ReadFileFromDisk(Directory.GetCurrentDirectory(), googleCredentialsFilename);
        var gsecrets = JsonConvert.DeserializeObject<GoogleClientSecrets>(credentials);

        var credential = await googleAuthService.AuthorizeAsync(gsecrets, storedCredentialsFilename);

        return new GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = nameof(GmailServiceFactory)
        });
    }
}
