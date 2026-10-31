using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SN.Application.Dtos;
using SN.Application.Interfaces;
using SN.Application.Metrics;
using SN.Application.Options;
using SN.Application.Services;
using SN.UnitTests.Fakes;
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
        var SUT = new SlackService(
            apiService, 
            slackBlockBuilder, 
            options, 
            NullLogger<SlackService>.Instance, 
            new SlackNotifierMetrics(new TestMeterFactory())
        );

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
        var SUT = new SlackService(
            apiService, 
            slackBlockBuilder, 
            options, 
            NullLogger<SlackService>.Instance, 
            new SlackNotifierMetrics(new TestMeterFactory())
        );

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
        var SUT = new SlackService(
            apiService, 
            slackBlockBuilder, 
            options, 
            NullLogger<SlackService>.Instance, 
            new SlackNotifierMetrics(new TestMeterFactory())
        );

        // Act
        var result = await SUT.SendMessage(emailInfosRequest);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessage_WhenSlackAcceptsMessageAndAttachment_ThenPostedEmailAndAttachmentAreRecorded()
    {
        // Assign
        var emailInfosRequest = CreateEmailInfosRequest();
        var expectedBytes = emailInfosRequest.Single().FileAttachments.Single().ToByteArray().Length;
        var apiService = SetupApiService(CreateHttpResponse(HttpStatusCode.OK));

        using var meterFactory = new TestMeterFactory();
        var SUT = new SlackService(apiService, SetupBuilder(), CreateOptions(), NullLogger<SlackService>.Instance, new SlackNotifierMetrics(meterFactory));

        // Each listener subscribes to one instrument and records every measurement from now on.
        using var emailsPostedListener = new MetricCollector<long>(meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.slack.emails.posted");
        using var lastSuccessListener = new MetricCollector<long>(meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.slack.last_success_timestamp");
        using var attachmentsUploadedListener = new MetricCollector<long>(meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.slack.attachments.uploaded");
        using var attachmentsSizeListener = new MetricCollector<long>(meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.slack.attachments.size");
        using var errorsListener = new MetricCollector<long>(meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.errors");

        // Act
        await SUT.SendMessage(emailInfosRequest);
        lastSuccessListener.RecordObservableInstruments();

        // Assert
        emailsPostedListener.GetMeasurementSnapshot().Should().ContainSingle().Which.Value.Should().Be(1);
        lastSuccessListener.GetMeasurementSnapshot().Should().ContainSingle().Which.Value.Should().BePositive();
        attachmentsUploadedListener.GetMeasurementSnapshot().Should().ContainSingle().Which.Value.Should().Be(1);
        attachmentsSizeListener.GetMeasurementSnapshot().Should().ContainSingle().Which.Value.Should().Be(expectedBytes);
        errorsListener.GetMeasurementSnapshot().Should().BeEmpty();
    }

    [Fact]
    public async Task SendMessage_WhenSlackRejectsMessage_ThenSlackErrorCodeIsRecordedAndEmailIsNotCountedAsPosted()
    {
        // Assign
        var apiService = SetupApiService(CreateHttpResponse(HttpStatusCode.OK, okValue: false, errorValue: "invalid_auth"));

        using var meterFactory = new TestMeterFactory();
        var SUT = new SlackService(apiService, SetupBuilder(), CreateOptions(), NullLogger<SlackService>.Instance, new SlackNotifierMetrics(meterFactory));

        // Each listener subscribes to one instrument and records every measurement from now on.
        using var emailsPostedListener = new MetricCollector<long>(meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.slack.emails.posted");
        using var lastSuccessListener = new MetricCollector<long>(meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.slack.last_success_timestamp");
        using var errorsListener = new MetricCollector<long>(meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.errors");

        // Act
        await SUT.SendMessage(CreateEmailInfosRequest());
        lastSuccessListener.RecordObservableInstruments();

        // Assert
        var error = errorsListener.GetMeasurementSnapshot().Should().ContainSingle().Subject;
        error.Tags["target"].Should().Be("slack");
        error.Tags["error.type"].Should().Be("invalid_auth");
        emailsPostedListener.GetMeasurementSnapshot().Should().BeEmpty();
        lastSuccessListener.GetMeasurementSnapshot().Should().BeEmpty();
    }

    [Fact]
    public async Task SendMessage_WhenSlackIsRateLimiting_ThenStatusCodeIsRecordedOnce()
    {
        // Assign
        var apiService = SetupApiService(CreateHttpResponse(HttpStatusCode.TooManyRequests, okValue: false, errorValue: "ratelimited"));

        using var meterFactory = new TestMeterFactory();
        var SUT = new SlackService(apiService, SetupBuilder(), CreateOptions(), NullLogger<SlackService>.Instance, new SlackNotifierMetrics(meterFactory));

        // Each listener subscribes to one instrument and records every measurement from now on.
        using var emailsPostedListener = new MetricCollector<long>(meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.slack.emails.posted");
        using var errorsListener = new MetricCollector<long>(meterFactory, SlackNotifierMetrics.MeterName, "slacknotifier.errors");

        // Act
        await SUT.SendMessage(CreateEmailInfosRequest());

        // Assert
        var error = errorsListener.GetMeasurementSnapshot().Should().ContainSingle().Subject;
        error.Tags["target"].Should().Be("slack");
        error.Tags["error.type"].Should().Be("429");
        emailsPostedListener.GetMeasurementSnapshot().Should().BeEmpty();
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

    private IOptions<SlackSecretsOptions> CreateOptions()
    {
        var secrets = new SlackSecretsOptions
        {
            Subject = "SlackService",
            Token = Fixture.Create<string>(),
            Destination = Fixture.Create<string>()
        };

        return Options.Create(secrets);
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

    private static HttpResponseMessage CreateHttpResponse(
        HttpStatusCode code, 
        string threadProperty = null, 
        string mailThreadId = null, 
        bool okValue = true, 
        string errorValue = null)
    {
        threadProperty ??= "ts";
        mailThreadId ??= "1";

        var json = new JsonObject { { threadProperty, mailThreadId }, { "ok", okValue } };
        if (errorValue is not null)
        {
            json.Add("error", errorValue);
        }

        return new HttpResponseMessage(code)
        {
            Content = new StringContent(json.ToString(), Encoding.UTF8)
        };
    }

    private ISlackApiService SetupApiService(HttpResponseMessage sendMessageResponse)
    {
        var okHttpResponse = CreateHttpResponse(HttpStatusCode.OK);
        var apiService = Fixture.Freeze<ISlackApiService>();
        apiService.SendMessage(Arg.Any<StringContent>()).Returns(Task.FromResult(sendMessageResponse));
        apiService
            .GetUploadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>())
            .Returns(Task.FromResult(CreateGetUploadUrlHttpResponse(HttpStatusCode.OK)));
        apiService.UploadFileAsync(Arg.Any<string>(), Arg.Any<Byte[]>())
            .Returns(Task.FromResult(okHttpResponse));
        apiService.CompleteUploadAsync(Arg.Any<Dictionary<string, string>>(), Arg.Any<string>())
            .Returns(Task.FromResult(okHttpResponse));
        return apiService;
    }
}
