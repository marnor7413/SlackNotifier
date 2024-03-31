using Google.Apis.Gmail.v1.Data;
using SN.Core.ValueObjects;

namespace MailService.Infrastructure.Extensions;

public class MessageTypeService : IMessageTypeService
{
    public bool IsMultiPartMixed(Message message)
    {
        return HasMimeType(message, MimeType.MultiPartMixed.Name) &&
            HasSubMimeType(message, MimeType.MultiPartAlternative.Name);
    }

    public bool IsMultiPartAlternativeMessage(Message message)
    {
        return HasMimeType(message, MimeType.MultiPartAlternative.Name) &&
            HasSubMimeType(message, MimeType.Text.Name) &&
            HasSubMimeType(message, MimeType.Html.Name) &&
            message.Payload.Parts.Count() == 2;
    }

    public bool IsMessageWithStupidIphoneAttachment(Message message)
    {
        return HasMimeType(message, MimeType.MultiPartMixed.Name) &&
            HasSubMimeType(message, MimeType.IphonePagesFileformat.Name);
    }

    public bool IsAPlainMessage(Message message)
    {
        return HasMimeType(message, MimeType.Text.Name) &&
            HasSubMimeType(message, MimeType.Text.Name) &&
            HasSubMimeType(message, MimeType.Text.Name) &&
            message.Payload.Parts.Count() == 2;
    }

    private bool HasMimeType(Message message, string mimeType)
        => message.Payload.MimeType == mimeType;

    private bool HasSubMimeType(Message message, string mimeType)
        => message.Payload.Parts.Any(x => x.MimeType == mimeType);
}