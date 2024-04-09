﻿using AutoFixture;
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
        
        var sendMessageResponse = CreateHttpResponse(HttpStatusCode.OK);
        var apiService = Fixture.Freeze<ISlackApiService>();
        apiService.SendMessage(Arg.Any<StringContent>()).Returns(Task.FromResult(sendMessageResponse));

        var uploadFileResponse = CreateHttpResponse(HttpStatusCode.OK);
        apiService.UploadFile(Arg.Any<MultipartFormDataContent>()).Returns(Task.FromResult(uploadFileResponse));
        
        var options = CreateOptions();
        var SUT = new SlackService(apiService, options);

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

        var options = CreateOptions();
        var SUT = new SlackService(apiService, options);

        // Act
        var result = await SUT.SendMessage(emailInfosRequest);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessage_WhenUploadFileThrowsException_ReturnsFalse()
    {
        // Assign
        var emailInfosRequest = CreateEmailInfosRequest();

        var sendMessageResponse = CreateHttpResponse(HttpStatusCode.OK);
        var apiService = Fixture.Freeze<ISlackApiService>();
        apiService.SendMessage(Arg.Any<StringContent>()).Returns(Task.FromResult(sendMessageResponse));

        apiService.UploadFile(Arg.Any<MultipartFormDataContent>()).Throws<Exception>();

        var options = CreateOptions();
        var SUT = new SlackService(apiService, options);

        // Act
        var result = await SUT.SendMessage(emailInfosRequest);

        // Assert
        result.Should().BeFalse();
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