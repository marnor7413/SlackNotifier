using Google.Apis.Gmail.v1.Data;

namespace MailService.Infrastructure.Extensions;

public interface IMessageTypeService
{
    bool IsAPlainMessage(Message message);
    bool IsMessageWithStupidIphoneAttachment(Message message);
    bool IsMultiPartAlternativeMessage(Message message);
    bool IsMultiPartMixed(Message message);
}