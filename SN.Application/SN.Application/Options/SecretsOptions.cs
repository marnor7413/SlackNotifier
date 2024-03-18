namespace MailService.ConsoleApp.Configuration;

public class SecretsOptions
{
    public string Subject { get; init; }
    public string Token { get; set; }
    public string Destination { get; set; }
}