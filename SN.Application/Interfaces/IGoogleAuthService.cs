using Google.Apis.Auth.OAuth2;

namespace SN.Application.Interfaces;

public interface IGoogleAuthService
{
    Task<UserCredential> AuthorizeAsync(GoogleClientSecrets gsecrets, string credPath);
}
