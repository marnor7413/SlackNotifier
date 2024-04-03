using AutoFixture;
using FluentAssertions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using MailService.Infrastructure.Factories;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using SN.Application.Interfaces;
using SN.UnitTests.Fakes;

namespace SN.UnitTests.SN.Infrastructure;

public class GmailServiceFactoryTests : BaseTests
{
    [Fact]
    public async Task ReturnsCorrectType()
    {
        // Assign
        var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection()
                .Build();
        var authService = Fixture.Freeze<IGoogleAuthService>();
        authService
            .AuthorizeAsync(Arg.Any<GoogleClientSecrets>(), Arg.Any<string>())
            .Returns(Task.FromResult(new FakeUserCredential("fakeuserid")).Result);

        var IOService = Fixture.Freeze<IIOService>();
        IOService
            .ReadFileFromDisk(Arg.Any<string>(), Arg.Any<string>())
            .Returns(CredentialsJson);
        var SUT = new GmailServiceFactory(configuration, authService, IOService);

        // Act
        var result = await SUT.GetService();

        // Assert
        result.Should().BeOfType<GmailService>();
    }
}