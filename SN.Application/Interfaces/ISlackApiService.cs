using SN.Core.ValueObjects;

namespace SN.Application.Interfaces;

public interface ISlackApiService
{
    Task<HttpResponseMessage> SendMessage(StringContent requestBody);
    Task<HttpResponseMessage> UploadFile(MultipartFormDataContent formData);
    Task<HttpResponseMessage> GetUploadUrlAsync(string fileType, string filename, long filesize);
    Task<HttpResponseMessage> UploadFileAsync(string uploadUrl, byte[] fileBytes);
    Task<HttpResponseMessage> CompleteUploadAsync(Dictionary<string, string> files, string messageThread);
}