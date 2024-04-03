namespace MailService.Infrastructure.EmailService;

public record FileAttachment(string FileName, string fileType, string Description, string Data)
{
    public byte[] ToByteArray()
    {
        var data = Base64UrlSafeStringToBase64Standard(Data);

        return Convert.FromBase64String(data);
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(FileName)) return false;
        if (string.IsNullOrWhiteSpace(fileType)) return false;
        if (string.IsNullOrWhiteSpace(Data)) return false;

        return true;
    }

    public static string Base64UrlSafeStringToBase64Standard(string data) => data.Replace("-", "+").Replace("_", "/").Replace(" ", "=");
}