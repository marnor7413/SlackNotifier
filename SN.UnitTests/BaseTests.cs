using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace SN.UnitTests;

public abstract class BaseTests
{
    protected Fixture Fixture;
    private string randomText = "abc";

    protected BaseTests()
    {
        Fixture = new Fixture();
        Fixture.Customize(new AutoNSubstituteCustomization());
        randomText = Fixture.Create<string>();
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
