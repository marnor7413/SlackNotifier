using MailService.ConsoleApp.Configuration;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace MailService.ConsoleApp.Extensions;

public static class LocalJsonSecrets
{
    public static List<SecretsOptions> GetJsonSecrets(this IConfiguration configuration, string relativeFilenamePath)
    {
        string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), relativeFilenamePath);
        string json = File.ReadAllText(jsonFilePath);
        
        return JsonConvert.DeserializeObject<List<SecretsOptions>>(json);
    }
}
