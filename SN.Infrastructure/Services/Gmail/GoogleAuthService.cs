using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Util.Store;
using SN.Application.Interfaces;

namespace SN.Infrastructure.Services.Gmail;

public class GoogleAuthService : IGoogleAuthService
{
    public async Task<UserCredential> AuthorizeAsync(GoogleClientSecrets gsecrets, string storedCredentialsFilename)
    {
        return await GoogleWebAuthorizationBroker.AuthorizeAsync(
            gsecrets.Secrets,
            new[]
            {
                GmailService.Scope.GmailModify,
                GmailService.Scope.GmailLabels,
                GmailService.Scope.MailGoogleCom
            },
            "user",
            CancellationToken.None,
            new FileDataStore(storedCredentialsFilename, true));
    }
}
