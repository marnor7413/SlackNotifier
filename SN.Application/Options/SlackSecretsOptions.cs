namespace SN.Application.Options;

public class SlackSecretsOptions
{
    public string Subject { get; init; }
    public string Token { get; set; }
    public string Destination { get; set; }
}