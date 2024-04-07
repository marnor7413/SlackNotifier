using Google.Apis.Gmail.v1.Data;
using SN.Application.Interfaces;
using SN.Core.ValueObjects;

namespace SN.Application.Services;

public class MessageTypeService : IMessageTypeService
{
    public bool IsMultiPartMixed(Message message)
    {
        return HasMimeType(message, MimeType.MultiPartMixed.Name) &&
            HasSubMimeType(message, MimeType.MultiPartAlternative.Name);
    }

    public bool IsMultiPartAlternativeMessage(Message message)
    {
        return HasMimeType(message, MimeType.MultiPartAlternative.Name);
    }

    public bool IsMessageWithIphonePagesAttachment(Message message)
    {
        return HasMimeType(message, MimeType.MultiPartMixed.Name) &&
            HasSubMimeType(message, MimeType.IphonePagesFileformat.Name);
    }

    public bool IsAPlainTextMessage(Message message)
    {
        return HasMimeType(message, MimeType.Text.Name);
    }

    private bool HasMimeType(Message message, string mimeType)
        => message.Payload.MimeType == mimeType;

    private bool HasSubMimeType(Message message, string mimeType)
        => message.Payload.Parts.Any(x => x.MimeType == mimeType);
}