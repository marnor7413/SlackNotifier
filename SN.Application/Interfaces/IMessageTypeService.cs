using Google.Apis.Gmail.v1.Data;

namespace MailService.Infrastructure.Extensions;

public interface IMessageTypeService
{
    bool IsAPlainTextMessage(Message message);
    bool IsMessageWithIphonePagesAttachment(Message message);
    bool IsMultiPartAlternativeMessage(Message message);
    bool IsMultiPartMixed(Message message);
}