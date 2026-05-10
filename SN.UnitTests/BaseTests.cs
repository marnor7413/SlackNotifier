using MimeKit;
using System.Text;

namespace SN.UnitTests;

public abstract class BaseTests : TestFixture
{

    protected static MimeMessage MockEmail(string id = "1")
    {
        var message = new MimeMessage();
        message.MessageId = id;
        message.Date = DateTimeOffset.Now;
        message.From.Add(new MailboxAddress("Martin Norén", "martinnoren@mymail.com"));
        message.Subject = "Any subject";

        var builder = new BodyBuilder
        {
            TextBody = "Hello world",
            HtmlBody = "<p>Hello world</p>"
        };

        var attachmentBytes = Encoding.UTF8.GetBytes("fake pdf content");
        builder.Attachments.Add("document.pdf", attachmentBytes, ContentType.Parse("application/pdf"));
        message.Body = builder.ToMessageBody();

        return message;
    }

    protected string CredentialsJson = @"
    {
      ""web"": {
        ""client_id"": """ + "xyz" + @""",
        ""project_id"": """ + "xyz" + @""",
        ""auth_uri"": """ + "xyz" + @""",
        ""token_uri"": """ + "xyz" + @""",
        ""auth_provider_x509_cert_url"": """ + "xyz" + @""",
        ""client_secret"": """ + "xyz" + @""",
        ""redirect_uris"": [
          ""http://localhost:8080/oauth2callback""
        ]
      }
    }";

    protected List<string> SupportedSlackFileFormats = new List<string>()
    {
        "text/plain",
        "application/x-iwork-pages-sffpages",
        "image/jpeg",
        "application/pdf",
        "image/bmp",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "image/gif",
        "text/csv",
        "image/png",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel.sheet.macroEnabled.12",
        "application/zip",
        "image/tiff",
        "application/rtf"
    }; 
}
