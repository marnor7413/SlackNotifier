using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using NSubstitute;

namespace SN.UnitTests.Fakes;

public class FakeUserCredential : UserCredential
{
    public FakeUserCredential(string userId)
           : base(new FakeAuthorizationCodeFlow(), userId, new TokenResponse()) { }
}

public class FakeAuthorizationCodeFlow : BaseTests, IAuthorizationCodeFlow
{
    public IAccessMethod AccessMethod => Substitute.For<IAccessMethod>();

    public IClock Clock => Substitute.For<IClock>();

    public IDataStore DataStore => Substitute.For<IDataStore>();

    public AuthorizationCodeRequestUrl CreateAuthorizationCodeRequest(string redirectUri)
    {
        throw new NotImplementedException();
    }

    public Task DeleteTokenAsync(string userId, CancellationToken taskCancellationToken)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Task<TokenResponse> ExchangeCodeForTokenAsync(string userId, string code, string redirectUri, CancellationToken taskCancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<TokenResponse> LoadTokenAsync(string userId, CancellationToken taskCancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<TokenResponse> RefreshTokenAsync(string userId, string refreshToken, CancellationToken taskCancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task RevokeTokenAsync(string userId, string token, CancellationToken taskCancellationToken)
    {
        throw new NotImplementedException();
    }

    public bool ShouldForceTokenRetrieval()
    {
        throw new NotImplementedException();
    }
}