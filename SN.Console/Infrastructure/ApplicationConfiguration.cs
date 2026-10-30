using SN.ConsoleApp.Constants;

namespace SN.ConsoleApp.Infrastructure;

public static class ApplicationConfiguration
{
    /// <summary> Environment value is only set in launchsettings.json for local debug</summary>
    public static string GetApplicationEnvironment()
    {
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == ApplicationConstants.Environments.Development;

        return isDevelopment
            ? ApplicationConstants.Environments.Development
            : ApplicationConstants.Environments.Production;
    }

}
