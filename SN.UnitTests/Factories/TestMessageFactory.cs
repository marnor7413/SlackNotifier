using AutoFixture;
using Google.Apis.Gmail.v1.Data;

namespace SN.UnitTests.Factories;

public class TestMessageFactory
{
    internal static string mimeTypePlainText = "text/plain";
    internal static string mimeTypeTextHtml = "text/html";
    internal static string mimeTypePdf = "application/pdf";
    internal static string mimeTypeMultipartAlternative = "multipart/alternative";
    internal static string mimeTypeMultipartMixed = "multipart/mixed";

    /// <summary>
    /// Hej,
    ///
    ///Bifogat finner ni kallelsen till stämma i garage samfälligheten.
    ///
    ///Allt gott,
    ///Jonas
    /// </summary>
    private static string base64Message1InPlainText = "SGVqLA0KDQpCaWZvZ2F0IGZpbm5lciBuaSBrYWxsZWxzZSB0aWxsIHN0w6RtbWEgaSBnYXJhZ2Ugc2FtZsOkbGxpZ2hldGVuLg0KDQpBbGx0IGdvdHQsDQpKb25hcw0K";
    private static string base64Message1InTextHtml = "PGRpdiBkaXI9Imx0ciI-SGVqLMKgPGRpdj48YnI-PC9kaXY-PGRpdj5CaWZvZ2F0IGZpbm5lciBuaSBrYWxsZWxzZSB0aWxsIHN0w6RtbWEgaSBnYXJhZ2Ugc2FtZsOkbGxpZ2hldGVuLsKgPC9kaXY-PGRpdj48YnI-PC9kaXY-PGRpdj5BbGx0IGdvdHQswqA8L2Rpdj48ZGl2PkpvbmFzPC9kaXY-PC9kaXY-DQo=";

    private static string base64PdfFileData = "ANGjdJ_Xph-DBVtQTdSisVqq-Vysf7tHPrKufE4D5-84kLRX5HS1ShhfMd6G48CaEUEEKokB8RDG1-QtKnvNjlRt5td60EBGKioDyXipMgtWE1xHENNIb3y2UOVxVLrGsuDK35CsX1BaQqBnrnaaJ3uev2yfuQp2SEmRDyE5qzcSuN_D_zEQ16p2HPhOrmWLE4B7lZOf4pP0F1Vd8Do1Qcj-KGE38VSV96I0Wmmz68M9Yb2pL9cu4cHYJAG-A9mhz0cBzHu6OwI5rAXUBv6VnxWxlM5WkgQ1ramaIODSEmpVQkiFqnAa588YlMvrmXNDvWVKeDqcwlXqX9NW_4wWa56eXEMCAjuknVgZwQ2X6hB5aZbkUMCNRyq1XzCOjJ9mkSLGm6KtD7R-TC98awZx";

    private static string fromSenderName = "Jonas Jonasson <jonas.Jonasson@gmail.com>";
    private static string fromText = "From";
    private static string subjectText = "Subject";
    private static string dateText = "Date";
    private static string todaysDateWithLocalOffset = DateTimeOffset.Now.ToString("ddd, dd MMM yyyy HH:mm:ss zzz");

    private static string contentTypeText = "Content-Type";
    private static string contentTransferEncodingText = "Content-Transfer-Encoding";

    private static string contentTypeMultipartAlternativeValueText = $"{mimeTypeMultipartAlternative}; boundary=\"0000000000001cfae90619214490\"";
    private static string contentTypePlainTextValueText = $"{mimeTypePlainText}; charset=\"UTF-8\"";
    private static string contentTypeTextHtmlValueText = $"{mimeTypeTextHtml}; charset=\"UTF-8\"";
    private static string contentTransferEncodingValueText = "quoted-printable";

    public static Message TextHtmlMessageWithAttachment(Fixture fixture)
    {
        var messageBodyText = fixture.Build<MessagePart>()
                        .Without(x => x.ETag)
                        .With(x => x.Body, MessagePartBody(fixture))
                        .With(x => x.Filename, string.Empty)
                        .With(x => x.MimeType, mimeTypeMultipartAlternative)
                        .With(x => x.PartId, "0")
                        .With(x => x.Headers, new List<MessagePartHeader>
                        {
                            MessagePartHeader(fixture, null, contentTypeText, contentTypeMultipartAlternativeValueText)
                        })
                        .With(x => x.Parts, new List<MessagePart>
                        {
                            MessageBodyInPlainTextFormat(fixture, base64Message1InPlainText),
                            MessageBodyInHtmlTextFormat(fixture, base64Message1InTextHtml)
                        })
                        .Create();

        var pdfFileName = fixture.Create<string>() + ".pdf";
        var fileAttachment = fixture.Build<MessagePart>()
                        .With(x => x.Body, MessagePartBody(fixture, null, base64PdfFileData))
                        .With(x => x.Filename, pdfFileName)
                        .With(x => x.MimeType, mimeTypePdf)
                        .With(x => x.Headers, new List<MessagePartHeader>
                        {
                            MessagePartHeader(fixture, null, contentTypeText, $"{mimeTypePdf}; name=\"{pdfFileName}\""),
                            MessagePartHeader(fixture, null, "Content-Disposition", $"attachment; filename=\"{pdfFileName}\""),
                            MessagePartHeader(fixture, null, contentTransferEncodingText, "base64"),
                            MessagePartHeader(fixture, null, "Content-ID", fixture.Create<string>())
                        })
                        .Without(x => x.Parts)
                        .Create();

        var message = fixture.Build<Message>()
            .With(x => x.Payload, fixture.Build<MessagePart>()
                .With(x => x.Body, MessagePartBody(fixture))
                .With(x => x.Filename, string.Empty)
                .With(x => x.MimeType, mimeTypeMultipartMixed)
                .With(x => x.Headers, MessagePartHeadersWithEmailSenderData(fixture))
                .With(x => x.Parts, new List<MessagePart>
                {
                    messageBodyText,
                    fileAttachment
                })
                .Create())
            .Create();

        return message;
    }

    private static MessagePart MessageBodyInPlainTextFormat(Fixture fixture, string messageData)
    {
        return fixture.Build<MessagePart>()
            .Without(x => x.Parts)
            .With(x => x.Body, MessagePartBody(fixture, null, messageData))
            .With(x => x.Filename, string.Empty)
            .With(x => x.MimeType, mimeTypePlainText)
            .With(x => x.PartId, "0.0")
            .With(x => x.Headers, new List<MessagePartHeader>
            {
                MessagePartHeader(fixture, null, contentTypeText, contentTypePlainTextValueText),
                MessagePartHeader(fixture, null, contentTransferEncodingText, contentTransferEncodingValueText)
            })
            .Create();
    }

    private static MessagePart MessageBodyInHtmlTextFormat(Fixture fixture, string messageData)
    {
        return fixture.Build<MessagePart>()
            .Without(x => x.Parts)
            .With(x => x.Body, MessagePartBody(fixture, null, messageData))
            .With(x => x.Filename, string.Empty)
            .With(x => x.MimeType, mimeTypeTextHtml)
            .With(x => x.PartId, "0.1")
            .With(x => x.Headers, new List<MessagePartHeader>
            {
                MessagePartHeader(fixture, null, contentTypeText, contentTypeTextHtmlValueText),
                MessagePartHeader(fixture, null, contentTransferEncodingText, contentTransferEncodingValueText),
            })
            .Create();
    }

    private static List<MessagePartHeader> MessagePartHeadersWithEmailSenderData(Fixture fixture)
    {
        return new List<MessagePartHeader>
                {
                    MessagePartHeader(fixture, null, fromText, fromSenderName),
                    MessagePartHeader(fixture, null, subjectText, fixture.Create<string>()),
                    MessagePartHeader(fixture, null, dateText, todaysDateWithLocalOffset)
                };
    }

    private static MessagePartBody MessagePartBody(Fixture fixture, string etag = null, string data = null, int? size = 0) 
    {
        return fixture.Build<MessagePartBody>()
            .With(x => x.AttachmentId, fixture.Create<string>())
            .With(x => x.ETag, etag)
            .With(x => x.Data, data)
            .With(x => x.Size, size)
            .Create();
    }

    private static MessagePartHeader MessagePartHeader(Fixture fixture, string etag = null, string name = null, string value = null)
    {
        return fixture.Build<MessagePartHeader>()
            .With(x => x.ETag, etag)
            .With(x => x.Name, name)
            .With(x => x.Value, value)
            .Create();
    }
}
