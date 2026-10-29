namespace SN.ConsoleApp.Constants;

public static class ApplicationConstants
{
    public const string Version = "1.3.3";

    public static class Environments
    {
        public const string Development = "Development";
        public const string Production = "Production";
    }

    public static class Appsettings
    {
        public const string GmailApiBaseUrlKey = "Appsettings:GmailBaseUri";
        public const string SlackApiBaseUrlKey = "Appsettings:SlackBaseUri";
    }
}
