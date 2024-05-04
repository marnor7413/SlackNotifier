namespace SN.Infrastructure.Services.Slack;

public static class SlackApiEndpoints
{
    public const string PostMessage = "/api/chat.postMessage";
    public const string GetUploadUrl = "/api/files.getUploadURLExternal";
    public const string CompleteUpload = "/api/files.completeUploadExternal?";
}
