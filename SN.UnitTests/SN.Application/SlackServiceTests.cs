using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SN.Application.Dtos;
using SN.Application.Interfaces;
using SN.Application.Options;
using SN.Application.Services;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace SN.UnitTests.SN.Application;

public class SlackServiceTests : BaseTests
{
    [Fact]
    public async Task SendMessage_MessageWithAttachmentAndStatusIsOkInResponses_ReturnsTrue()
    {
        // Assign
        var emailInfosRequest = CreateEmailInfosRequest();

        var okHttpResponse = CreateHttpResponse(HttpStatusCode.OK);
        var apiService = Fixture.Freeze<ISlackApiService>();
        apiService.SendMessage(Arg.Any<StringContent>()).Returns(Task.FromResult(okHttpResponse));

        var getUploadUrlResponse = CreateGetUploadUrlHttpResponse(HttpStatusCode.OK);
        apiService
            .GetUploadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>())
            .Returns(Task.FromResult(getUploadUrlResponse));
        apiService.UploadFileAsync(Arg.Any<string>(), Arg.Any<Byte[]>())
            .Returns(Task.FromResult(okHttpResponse));
        apiService.CompleteUploadAsync(Arg.Any<Dictionary<string, string>>(), Arg.Any<string>())
            .Returns(Task.FromResult(okHttpResponse));
        
        var slackBlockBuilder = SetupBuilder();
        var options = CreateOptions();
        var SUT = new SlackService(apiService, slackBlockBuilder, options);

        // Act
        var result = await SUT.SendMessage(emailInfosRequest);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendMessage_WhenSendMessageThrowsException_ReturnsFalse()
    {
        // Assign
        var emailInfosRequest = CreateEmailInfosRequest();

        var apiService = Fixture.Freeze<ISlackApiService>();
        apiService.SendMessage(Arg.Any<StringContent>()).Throws<Exception>();

        var slackBlockBuilder = SetupBuilder();
        var options = CreateOptions();
        var SUT = new SlackService(apiService, slackBlockBuilder, options);

        // Act
        var result = await SUT.SendMessage(emailInfosRequest);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessage_WhenGetUploadUrlAsyncThrowsException_ReturnsFalse()
    {
        // Assign
        var emailInfosRequest = CreateEmailInfosRequest();

        var sendMessageResponse = CreateHttpResponse(HttpStatusCode.OK);
        var apiService = Fixture.Freeze<ISlackApiService>();
        apiService
            .SendMessage(Arg.Any<StringContent>())
            .Returns(Task.FromResult(sendMessageResponse));
        apiService
            .GetUploadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>())
            .Throws<Exception>();

        var slackBlockBuilder = SetupBuilder();
        var options = CreateOptions();
        var SUT = new SlackService(apiService, slackBlockBuilder, options);

        // Act
        var result = await SUT.SendMessage(emailInfosRequest);

        // Assert
        result.Should().BeFalse();
    }

    private ISlackBlockBuilder SetupBuilder()
    {
        var slackBlockBuilder = Fixture.Freeze<ISlackBlockBuilder>();
        slackBlockBuilder.WithDivider().Returns(slackBlockBuilder);
        slackBlockBuilder.WithHeaderTitle(Arg.Any<string>()).Returns(slackBlockBuilder);
        slackBlockBuilder.WithMessageBody(Arg.Any<string>()).Returns(slackBlockBuilder);
        slackBlockBuilder.WithSendDate(Arg.Any<string>()).Returns(slackBlockBuilder);
        slackBlockBuilder.ToChannel(Arg.Any<string>()).Returns(slackBlockBuilder);
        slackBlockBuilder.FromSender(Arg.Any<string>()).Returns(slackBlockBuilder);
        slackBlockBuilder.WithSubject(Arg.Any<string>()).Returns(slackBlockBuilder);
        slackBlockBuilder.WithRelatedFiles(Arg.Any<Dictionary<string, string>>()).Returns(slackBlockBuilder);
        slackBlockBuilder.Build().Returns(new StringContent(new JsonObject().ToString(), Encoding.UTF8));
        return slackBlockBuilder;
    }

    private List<EmailInfo> CreateEmailInfosRequest()
    {
        var emailInfosRequest = new List<EmailInfo>()
        {
            new EmailInfo(1,
            DateTime.Now.ToString(),
            "Martin Norén <martin.noren@gmail.com>",
            "Testmail subject",
            "Testmail body text",
            "Testmail body html")
        };
        emailInfosRequest.Single().FileAttachments.Add(Fixture.Create<FileAttachment>());
        return emailInfosRequest;
    }

    private IOptions<List<SecretsOptions>> CreateOptions()
    {
        var options = Substitute.For<IOptions<List<SecretsOptions>>>();
        var secrets = new List<SecretsOptions>
        {
            new SecretsOptions
            {
                Subject = "SlackService",
                Token = Fixture.Create<string>(),
                Destination = Fixture.Create<string>()
            }
        };
        options.Value.Returns(secrets);
        return options;
    }

    private static HttpResponseMessage CreateGetUploadUrlHttpResponse(HttpStatusCode code, 
        bool okValue = true, 
        string errorValue = "",
        string uploadUrlValue = "http://anywhere.com",
        string fileIdValue = "file.txt")
    {
        var okProperty = "ok";
        var errorProperty = "error";
        var uploadUrlProperty = "upload_url";
        var fileIdProperty = "file_id";
        
        return new HttpResponseMessage(code)
        {
            Content = new StringContent(
                        new JsonObject 
                        { 
                            { okProperty, okValue },
                            { errorProperty, errorValue },
                            { uploadUrlProperty, uploadUrlValue },
                            { fileIdProperty, fileIdValue }
                            
                        }.ToString(),
                        Encoding.UTF8)
        };
    }
    
    private static HttpResponseMessage CreateHttpResponse(HttpStatusCode code, string threadProperty = null, string mailThreadId = null)
    {
        threadProperty ??= "ts";
        mailThreadId ??= "1";
        
        return new HttpResponseMessage(code)
        {
            Content = new StringContent(
                        new JsonObject { { threadProperty, mailThreadId } }.ToString(),
                        Encoding.UTF8)
        };
    }
}
