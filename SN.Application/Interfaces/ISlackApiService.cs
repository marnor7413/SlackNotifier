namespace SN.Application.Interfaces;

public interface ISlackApiService
{
    Task<HttpResponseMessage> SendMessage(StringContent requestBody);
    Task<HttpResponseMessage> UploadFile(MultipartFormDataContent formData);
}