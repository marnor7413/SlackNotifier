namespace SN.Application.Extensions;

static class HttpResponseMessageExtensions
{
    public static async Task<dynamic> ExtractResponseDataFromHttpResponseMessage(this HttpResponseMessage message)
    {
        return Newtonsoft.Json.JsonConvert
            .DeserializeObject<dynamic>(await message.Content.ReadAsStringAsync());
    }
}