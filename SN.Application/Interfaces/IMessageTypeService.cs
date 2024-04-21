using Google.Apis.Gmail.v1.Data;

namespace SN.Application.Interfaces;

public interface IMessageTypeService
{
    bool IsAPlainTextMessage(Message message);
    bool IsMessageWithIphonePagesAttachment(Message message);
    bool IsMultiPartAlternativeMessage(Message message);
    bool IsMultiPartMixed(Message message);
    bool IsMultiPartRelated(Message message);
    bool IsMultiPartMixedAndMultiPartRelated(Message message);
}