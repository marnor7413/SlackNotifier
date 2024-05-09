using System.Text.Json.Serialization;
using System.Text.Json;

namespace SN.Core.ValueObjects;

public record SlackGetUploadUrlResponse()
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; }

    [JsonPropertyName("upload_url")]
    public string UploadUrl { get; set; }

    [JsonPropertyName("file_id")]
    public string FileId { get; set; }

    public static SlackGetUploadUrlResponse FromJson(string json)
    {
        return JsonSerializer.Deserialize<SlackGetUploadUrlResponse>(json);
    }
};
