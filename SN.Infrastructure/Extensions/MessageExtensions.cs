using Google.Apis.Gmail.v1.Data;

namespace MailService.Infrastructure.Extensions;

static class MessageExtensions
{
    public static bool HasMimeType(this Message message, string mimeType)
        => message.Payload.MimeType == mimeType;
    public static bool HasSubMimeType(this Message message, string mimeType)
        => message.Payload.Parts.Any(x => x.MimeType == mimeType);
}