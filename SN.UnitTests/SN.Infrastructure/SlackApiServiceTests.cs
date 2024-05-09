using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SN.Application.Options;
using SN.Application.Services;
using SN.Infrastructure.Services.Slack;
using SN.UnitTests.Fakes;
using System.Net;

namespace SN.UnitTests.SN.Infrastructure;

public class SlackApiServiceTests : BaseTests
{
    [Fact]
    public async Task UploadFile_WithValidFormData_ReturnsSuccess()
    {
        // Arrange
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var httpClientFactory = new FakeHttpClientFactory(request =>
        {
            return expectedResponse;
        });

        var formData = new MultipartFormDataContent();
        var SUT = CreateSUT(httpClientFactory);

        // Act
        var response = await SUT.GetUploadUrlAsync(Fixture.Create<string>(), Fixture.Create<string>(), Fixture.Create<long>());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UploadFile_WithValidFormData_ReturnsSuccessAndHasAuthorizationHeader()
    {
        // Arrange
        HttpRequestMessage capturedRequest = null;
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var httpClientFactory = new FakeHttpClientFactory(request =>
        {
            capturedRequest = request;
           
            return expectedResponse;
        });

        var formData = new MultipartFormDataContent();
        var SUT = CreateSUT(httpClientFactory);

        // Act
        var response = await SUT.GetUploadUrlAsync(Fixture.Create<string>(), Fixture.Create<string>(), Fixture.Create<int>());

        // Assert
        Assert.True(capturedRequest.Headers.Contains("Authorization"));
        string.Equals("Bearer testToken",capturedRequest.Headers.Authorization.ToString(), StringComparison.OrdinalIgnoreCase)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task SendMessage_WithValidStringContent_ReturnsSuccess()
    {
        // Arrange
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var httpClientFactory = new FakeHttpClientFactory(request =>
        {
            return expectedResponse;
        });

        var stringContent = new StringContent(Fixture.Create<string>());
        var SUT = CreateSUT(httpClientFactory);


        // Act
        var response = await SUT.SendMessage(stringContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_WithValidFormData_ReturnsSuccessAndHasAuthorizationHeader()
    {
        // Arrange
        HttpRequestMessage capturedRequest = null;
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var httpClientFactory = new FakeHttpClientFactory(request =>
        {
            capturedRequest = request;

            return expectedResponse;
        });

        var stringContent = new StringContent(Fixture.Create<string>());
        var SUT = CreateSUT(httpClientFactory);

        // Act
        var response = await SUT.SendMessage(stringContent);

        // Assert
        Assert.True(capturedRequest.Headers.Contains("Authorization"));
        string.Equals("Bearer testToken", capturedRequest.Headers.Authorization.ToString(), StringComparison.OrdinalIgnoreCase)
            .Should()
            .BeTrue();
    }

    private static SlackApiService CreateSUT(FakeHttpClientFactory httpClientFactory)
    {
        return new SlackApiService(
            Options.Create(new List<SecretsOptions>
            {
                new SecretsOptions
                {
                    Subject = nameof(SlackService),
                    Token = "testToken"
                }
            }),
            httpClientFactory);
    }
}
