namespace SN.UnitTests.Fakes;

public class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public FakeHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name)
    {
        HttpClient httpClient = new HttpClient(new FakeHttpMessageHandler(_handler));
        httpClient.BaseAddress = new Uri("https://anyhost.com");
        return httpClient;
    }
}
