using Google.Apis.Gmail.v1;

namespace SN.Application.Interfaces;

public interface IGmailServiceFactory
{
    Task<GmailService> GetService();
}